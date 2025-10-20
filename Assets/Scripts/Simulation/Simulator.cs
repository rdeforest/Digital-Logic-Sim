using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using DLS.Description;
using DLS.Game;
using DLS.Simulation.ChipProcessors;
using Random = System.Random;

namespace DLS.Simulation
{
	public static class Simulator
	{
		public static readonly Random rng = new();
		static readonly Stopwatch stopwatch = Stopwatch.StartNew();
		public static int stepsPerClockTransition;
		public static int simulationFrame;
		static uint pcg_rngState;

		public static bool canDynamicReorderThisFrame;
		public static bool debug_deterministicMode = false;

		static SimChip prevRootSimChip;
		static double elapsedSecondsOld;
		static double deltaTime;
		static SimAudio audioState;

		// Modifications to the sim are made from the main thread, but only applied on the sim thread to avoid conflicts
		static readonly ConcurrentQueue<SimModifyCommand> modificationQueue = new();

		// Flat list of all primitive (non-custom) chips in the current circuit
		static System.Collections.Generic.List<SimChip> allPrimitiveChips = new();

		// Topologically sorted list of primitives (computed once when circuit loads)
		// Chips are ordered so dependencies come before dependents (as much as possible)
		// Chips in feedback loops are placed in arbitrary but consistent order
		static System.Collections.Generic.List<SimChip> sortedPrimitives = new();

		/// <summary>

		// ---- Simulation outline ----
		// 1) Forward the initial player-controlled input states to all connected pins.
		// 2) Loop over all subchips not yet processed this frame, and process them if they are ready (i.e. all input pins have received all their inputs)
		//    * Note: this means that the input pins must be aware of how many input connections they have (pins choose randomly between conflicting inputs)
		//    * Note: if a pin has zero input connections, it should be considered as always ready
		// 3) Forward the outputs of the processed subchips to their connected pins, and repeat steps 2 & 3 until no more subchips are ready for processing.
		// 4) If all subchips have now been processed, then we're done. This is not necessarily the case though, since if an input pin depends on the output of its parent chip
		//    (directly or indirectly), then it won't receive all its inputs until the chip has already been run, meaning that the chip must be processed before it is ready.
		//    In this case we process one of the remaining unprocessed (and non-ready) subchips at random, and return to step 3.
		//
		// Optimization ideas (todo):
		// * Compute lookup table for combinational chips
		// * Ignore chip if inputs are same as last frame, and no internal pins changed state last frame.
		//   (would have to make exception for chips containing things like clock or key chip, which can activate 'spontaneously')
		// * Create simplified connections network allowing only builtin chips to be processed during simulation

		public static void RunSimulationStep(SimChip rootSimChip, DevPinInstance[] inputPins, SimAudio audioState)
		{
			Simulator.audioState = audioState;
			audioState.InitFrame();

			HandleRootChipChange(rootSimChip);

			pcg_rngState                = (uint)rng.Next();
			canDynamicReorderThisFrame  = simulationFrame % 100 == 0;

			CopyPlayerInputsToSim(rootSimChip, inputPins);

			// Propagate root chip inputs through the circuit hierarchy
			PropagateRootInputs(rootSimChip);

			// Process all chips in topologically sorted order
			// This ensures dependencies are evaluated before dependents
			// Chips in feedback loops use values from the previous step
			foreach (SimChip chip in sortedPrimitives)
			{
				chip.StepChip();
			}

			CompleteStep();

			UpdateAudioState();
		}

        public static void UpdateInPausedState()
		{
			if (audioState != null)
			{
				audioState.InitFrame();
				UpdateAudioState();
			}
		}

		static void UpdateAudioState()
		{
			double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
			if (simulationFrame <= 1) deltaTime = 0;
			else deltaTime = elapsedSeconds - elapsedSecondsOld;
			elapsedSecondsOld = stopwatch.Elapsed.TotalSeconds;
			audioState.NotifyAllNotesRegistered(deltaTime);
		}

		static void HandleRootChipChange(SimChip rootSimChip)
		{
			if (rootSimChip != prevRootSimChip)
			{
				prevRootSimChip = rootSimChip;

				// Build flat list of all primitive chips
				allPrimitiveChips.Clear();
				CollectPrimitiveChips(rootSimChip, allPrimitiveChips);

				// Topologically sort primitives for optimal execution order
				sortedPrimitives = TopologicalSort(allPrimitiveChips);
			}
		}

		/// <summary>
		/// Recursively collect all primitive (non-custom) chips from a chip hierarchy.
		/// Custom chips are just containers - we flatten them to get the actual logic chips.
		/// </summary>
		static void CollectPrimitiveChips(SimChip chip, System.Collections.Generic.List<SimChip> primitives)
		{
			foreach (SimChip subChip in chip.SubChips)
			{
				if (subChip.ChipType == ChipType.Custom)
				{
					// Custom chip - recurse to find primitives inside
					CollectPrimitiveChips(subChip, primitives);
				}
				else
				{
					// Primitive chip - add to flat list
					primitives.Add(subChip);
				}
			}
		}

		/// <summary>
		/// Recursively propagate root chip's inputs through all custom chip containers.
		/// This ensures that when you set an input pin on the root chip, it flows through
		/// the custom chip hierarchy to reach the actual primitive chips inside.
		/// </summary>
		static void PropagateRootInputs(SimChip chip)
		{
			// Propagate this chip's inputs to its internal connections
			chip.Sim_PropagateInputs();

			// Recursively propagate through custom subchips
			foreach (SimChip subChip in chip.SubChips)
			{
				if (subChip.ChipType == ChipType.Custom)
				{
					PropagateRootInputs(subChip);
				}
			}
		}

		/// <summary>
		/// Topologically sort chips using Kahn's algorithm (BFS-based).
		/// This gives breadth-first ordering: chips at the same "level" from inputs process together.
		/// Chips are ordered so dependencies come before dependents.
		/// Chips in feedback loops are handled by processing chips with satisfied dependencies first.
		/// </summary>
		static System.Collections.Generic.List<SimChip> TopologicalSort(System.Collections.Generic.List<SimChip> chips)
		{
			var sorted = new System.Collections.Generic.List<SimChip>();
			var inDegree = new System.Collections.Generic.Dictionary<SimChip, int>();
			var queue = new System.Collections.Generic.Queue<SimChip>();

			// Calculate in-degree for each chip (number of dependencies)
			foreach (SimChip chip in chips)
			{
				int numDependencies = 0;
				foreach (SimPin inputPin in chip.InputPins)
				{
					foreach (SimPin sourcePin in GetSourcePinsForSort(inputPin))
					{
						// Skip self-loops - chip doesn't depend on itself for initial ordering
						if (sourcePin.parentChip != chip)
						{
							numDependencies++;
							break; // One dependency per input pin is enough for counting
						}
					}
				}
				inDegree[chip] = numDependencies;

				// Chips with no dependencies start in the queue
				if (numDependencies == 0)
				{
					queue.Enqueue(chip);
				}
			}

			// Process chips in breadth-first order
			while (queue.Count > 0)
			{
				SimChip chip = queue.Dequeue();
				sorted.Add(chip);

				// Reduce in-degree of dependent chips
				foreach (SimPin outputPin in chip.OutputPins)
				{
					foreach (SimPin targetPin in outputPin.ConnectedTargetPins)
					{
						SimChip dependent = targetPin.parentChip;

						// Skip self-loops
						if (dependent == chip)
							continue;

						if (inDegree.ContainsKey(dependent))
						{
							inDegree[dependent]--;
							if (inDegree[dependent] == 0)
							{
								queue.Enqueue(dependent);
							}
						}
					}
				}
			}

			// Handle cycles: any chips not yet sorted are in cycles
			// Add them in arbitrary order
			foreach (SimChip chip in chips)
			{
				if (!sorted.Contains(chip))
				{
					sorted.Add(chip);
				}
			}

			return sorted;
		}

		/// <summary>
		/// Get source pins for topological sort. Similar to GetSourcePins but optimized
		/// for the one-time sort operation.
		/// </summary>
		static System.Collections.Generic.IEnumerable<SimPin> GetSourcePinsForSort(SimPin inputPin)
		{
			// Search through all primitives to find who connects to this input
			foreach (SimChip primitive in allPrimitiveChips)
			{
				foreach (SimPin outputPin in primitive.OutputPins)
				{
					if (Array.Exists(outputPin.ConnectedTargetPins, target => target == inputPin))
					{
						yield return outputPin;
					}
				}
			}
		}

		static void CopyPlayerInputsToSim(SimChip rootSimChip, DevPinInstance[] inputPins)
		{
			foreach (DevPinInstance input in inputPins)
			{
				try
				{
					SimPin simPin    = rootSimChip.GetSimPinFromAddress(input.Pin.Address);
					uint simPinState = simPin.State;
					PinState.Set(ref simPinState, input.Pin.PlayerInputState);
					simPin.State = simPinState;

					input.Pin.State = input.Pin.PlayerInputState;
				}
				catch (Exception)
				{
				}
			}
		}



		static void CompleteStep()
		{
			simulationFrame++;
		}


		public static void UpdateKeyboardInputFromMainThread()
		{
			SimKeyboardHelper.RefreshInputState();
		}

		public static bool RandomBool()
		{
			pcg_rngState = pcg_rngState * 747796405 + 2891336453;
			uint result = ((pcg_rngState >> (int)((pcg_rngState >> 28) + 4)) ^ pcg_rngState) * 277803737;
			result = (result >> 22) ^ result;
			return result < uint.MaxValue / 2;
		}


		public static SimChip BuildSimChip(ChipDescription chipDesc, ChipLibrary library)
		{
			return BuildSimChip(chipDesc, library, -1, null);
		}

		public static SimChip BuildSimChip(ChipDescription chipDesc, ChipLibrary library, int subChipID, uint[] internalState)
		{
			SimChip simChip = BuildSimChipRecursive(chipDesc, library, subChipID, internalState);
			return simChip;
		}

		// Recursively build full representation of chip from its description for simulation.
		static SimChip BuildSimChipRecursive(ChipDescription chipDesc, ChipLibrary library, int subChipID, uint[] internalState)
		{
			// Recursively create subchips
			SimChip[] subchips = chipDesc.SubChips.Length == 0 ? Array.Empty<SimChip>() : new SimChip[chipDesc.SubChips.Length];

			for (int i = 0; i < chipDesc.SubChips.Length; i++)
			{
				SubChipDescription subchipDesc = chipDesc.SubChips[i];
				ChipDescription subchipFullDesc = library.GetChipDescription(subchipDesc.Name);
				SimChip subChip = BuildSimChipRecursive(subchipFullDesc, library, subchipDesc.ID, subchipDesc.InternalData);
				subchips[i] = subChip;
			}

			SimChip simChip = new(chipDesc, subChipID, internalState, subchips);


			// Create connections
			for (int i = 0; i < chipDesc.Wires.Length; i++)
			{
				simChip.AddConnection(chipDesc.Wires[i].SourcePinAddress, chipDesc.Wires[i].TargetPinAddress);
			}

			return simChip;
		}

		public static void AddPin(SimChip simChip, int pinID, bool isInputPin)
		{
			SimModifyCommand command = new()
			{
				type = SimModifyCommand.ModificationType.AddPin,
				modifyTarget = simChip,
				simPinToAdd = new SimPin(pinID, isInputPin, simChip),
				pinIsInputPin = isInputPin
			};
			modificationQueue.Enqueue(command);
		}

		public static void RemovePin(SimChip simChip, int pinID)
		{
			SimModifyCommand command = new()
			{
				type = SimModifyCommand.ModificationType.RemovePin,
				modifyTarget = simChip,
				removePinID = pinID
			};
			modificationQueue.Enqueue(command);
		}

		public static void AddSubChip(SimChip simChip, ChipDescription desc, ChipLibrary chipLibrary, int subChipID, uint[] subChipInternalData)
		{
			SimModifyCommand command = new()
			{
				type = SimModifyCommand.ModificationType.AddSubchip,
				modifyTarget = simChip,
				chipDesc = desc,
				lib = chipLibrary,
				subChipID = subChipID,
				subChipInternalData = subChipInternalData
			};
			modificationQueue.Enqueue(command);
		}

		public static void AddConnection(SimChip simChip, PinAddress source, PinAddress target)
		{
			SimModifyCommand command = new()
			{
				type = SimModifyCommand.ModificationType.AddConnection,
				modifyTarget = simChip,
				sourcePinAddress = source,
				targetPinAddress = target
			};
			modificationQueue.Enqueue(command);
		}

		public static void RemoveConnection(SimChip simChip, PinAddress source, PinAddress target)
		{
			SimModifyCommand command = new()
			{
				type = SimModifyCommand.ModificationType.RemoveConnection,
				modifyTarget = simChip,
				sourcePinAddress = source,
				targetPinAddress = target
			};
			modificationQueue.Enqueue(command);
		}

		public static void RemoveSubChip(SimChip simChip, int id)
		{
			SimModifyCommand command = new()
			{
				type = SimModifyCommand.ModificationType.RemoveSubChip,
				modifyTarget = simChip,
				removeSubChipID = id
			};
			modificationQueue.Enqueue(command);
		}

		// Note: this should only be called from the sim thread
		public static void ApplyModifications()
		{
			while (modificationQueue.Count > 0)
			{
				if (modificationQueue.TryDequeue(out SimModifyCommand cmd))
				{
					if (cmd.type == SimModifyCommand.ModificationType.AddSubchip)
					{
						SimChip newSubChip = BuildSimChip(cmd.chipDesc, cmd.lib, cmd.subChipID, cmd.subChipInternalData);
						cmd.modifyTarget.AddSubChip(newSubChip);
					}
					else if (cmd.type == SimModifyCommand.ModificationType.RemoveSubChip)
					{
						cmd.modifyTarget.RemoveSubChip(cmd.removeSubChipID);
					}
					else if (cmd.type == SimModifyCommand.ModificationType.AddConnection)
					{
						cmd.modifyTarget.AddConnection(cmd.sourcePinAddress, cmd.targetPinAddress);
					}
					else if (cmd.type == SimModifyCommand.ModificationType.RemoveConnection)
					{
						cmd.modifyTarget.RemoveConnection(cmd.sourcePinAddress, cmd.targetPinAddress);
					}
					else if (cmd.type == SimModifyCommand.ModificationType.AddPin)
					{
						cmd.modifyTarget.AddPin(cmd.simPinToAdd, cmd.pinIsInputPin);
					}
					else if (cmd.type == SimModifyCommand.ModificationType.RemovePin)
					{
						cmd.modifyTarget.RemovePin(cmd.removePinID);
					}
				}
			}
		}

		public static void Reset()
		{
			simulationFrame = 0;
			modificationQueue?.Clear();
			stopwatch.Restart();
			elapsedSecondsOld = 0;
		}

		struct SimModifyCommand
		{
			public enum ModificationType
			{
				AddSubchip,
				RemoveSubChip,
				AddConnection,
				RemoveConnection,
				AddPin,
				RemovePin
			}

			public ModificationType type;
			public SimChip modifyTarget;
			public ChipDescription chipDesc;
			public ChipLibrary lib;
			public int subChipID;
			public uint[] subChipInternalData;
			public PinAddress sourcePinAddress;
			public PinAddress targetPinAddress;
			public SimPin simPinToAdd;
			public bool pinIsInputPin;
			public int removePinID;
			public int removeSubChipID;
		}
	}
}
