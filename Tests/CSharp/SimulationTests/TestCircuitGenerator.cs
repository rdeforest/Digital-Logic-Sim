using System;
using System.Collections.Generic;
using DLS.Description;

namespace DLS.SimulationTests
{
	/// <summary>
	/// Generates test circuits programmatically for benchmarking.
	/// Creates circuits with known structural properties (depth, width, feedback loops).
	/// </summary>
	public static class TestCircuitGenerator
	{
		/// <summary>
		/// Create a simple chain of NOT gates.
		/// Tests basic linear propagation and minimal branching.
		///
		/// Structure: Input -> NOT -> NOT -> ... -> NOT -> Output
		///
		/// Properties:
		/// - Depth: length
		/// - Width: 1
		/// - Primitives: length
		/// - Feedback loops: 0
		/// </summary>
		public static ChipDescription CreateNotChain(string name, int length)
		{
			var chip = new ChipDescription { Name = name };

			// Add single input and output
			chip.InputPins = new[] { new PinDescription { ID = 0, Name = "In", BitCount = BitCount.One } };
			chip.OutputPins = new[] { new PinDescription { ID = 0, Name = "Out", BitCount = BitCount.One } };

			var subChips = new List<SubChipDescription>();
			var wires = new List<WireDescription>();

			// Create chain of NOT gates
			for (int i = 0; i < length; i++)
			{
				subChips.Add(new SubChipDescription
				{
					Name = "Nand",
					ID = i,
					Label = $"NOT{i}"
				});

				if (i == 0)
				{
					// Wire input to first NOT gate
					wires.Add(CreateWire(
						-1, 0,  // From chip input pin 0
						i, 0    // To first NAND's input 0
					));
					wires.Add(CreateWire(
						-1, 0,  // From chip input pin 0
						i, 1    // To first NAND's input 1 (both inputs = NOT)
					));
				}
				else
				{
					// Wire previous NOT output to this NOT input
					wires.Add(CreateWire(
						i - 1, 0,  // From previous NAND's output
						i, 0       // To current NAND's input 0
					));
					wires.Add(CreateWire(
						i - 1, 0,  // From previous NAND's output
						i, 1       // To current NAND's input 1
					));
				}
			}

			// Wire last NOT gate to output
			wires.Add(CreateWire(
				length - 1, 0,  // From last NAND's output
				-1, 0           // To chip output pin 0
			));

			chip.SubChips = subChips.ToArray();
			chip.Wires = wires.ToArray();

			return chip;
		}

		/// <summary>
		/// Create a binary tree of AND gates.
		/// Tests wide fanout and reconvergent paths.
		///
		/// Structure: Multiple inputs combine through AND gates in tree structure
		///
		/// Properties:
		/// - Depth: log2(inputCount)
		/// - Width: inputCount/2 at widest level
		/// - Primitives: inputCount - 1 (AND gates)
		/// - Feedback loops: 0
		/// </summary>
		public static ChipDescription CreateAndTree(string name, int inputCount)
		{
			if ((inputCount & (inputCount - 1)) != 0)
			{
				throw new ArgumentException("inputCount must be a power of 2");
			}

			var chip = new ChipDescription { Name = name };

			// Add inputs
			var inputs = new PinDescription[inputCount];
			for (int i = 0; i < inputCount; i++)
			{
				inputs[i] = new PinDescription { ID = i, Name = $"In{i}", BitCount = BitCount.One };
			}
			chip.InputPins = inputs;

			// Add single output
			chip.OutputPins = new[] { new PinDescription { ID = 0, Name = "Out", BitCount = BitCount.One } };

			var subChips = new List<SubChipDescription>();
			var wires = new List<WireDescription>();

			int nextChipID = 0;
			int currentLevel = inputCount;

			// Build tree bottom-up
			var currentLevelOutputs = new List<(int chipID, int pinID)>();

			// Initialize with inputs
			for (int i = 0; i < inputCount; i++)
			{
				currentLevelOutputs.Add((-1, i)); // -1 means root chip input
			}

			while (currentLevel > 1)
			{
				var nextLevelOutputs = new List<(int chipID, int pinID)>();

				for (int i = 0; i < currentLevelOutputs.Count; i += 2)
				{
					// Create AND gate (using NANDs)
					int nand1ID = nextChipID++;
					int nand2ID = nextChipID++;

					subChips.Add(new SubChipDescription { Name = "Nand", ID = nand1ID, Label = $"NAND{nand1ID}" });
					subChips.Add(new SubChipDescription { Name = "Nand", ID = nand2ID, Label = $"NOT{nand2ID}" });

					// Wire inputs to first NAND
					var (leftChipID, leftPinID) = currentLevelOutputs[i];
					var (rightChipID, rightPinID) = currentLevelOutputs[i + 1];

					wires.Add(CreateWire(leftChipID, leftPinID, nand1ID, 0));
					wires.Add(CreateWire(rightChipID, rightPinID, nand1ID, 1));

					// Wire NAND output to NOT (to make AND)
					wires.Add(CreateWire(nand1ID, 0, nand2ID, 0));
					wires.Add(CreateWire(nand1ID, 0, nand2ID, 1));

					nextLevelOutputs.Add((nand2ID, 0));
				}

				currentLevelOutputs = nextLevelOutputs;
				currentLevel /= 2;
			}

			// Wire final output
			wires.Add(CreateWire(currentLevelOutputs[0].chipID, currentLevelOutputs[0].pinID, -1, 0));

			chip.SubChips = subChips.ToArray();
			chip.Wires = wires.ToArray();

			return chip;
		}

		/// <summary>
		/// Create an SR latch (simple feedback loop).
		/// Tests sequential logic and feedback handling.
		///
		/// Properties:
		/// - Depth: 2 (two levels of NOR gates)
		/// - Width: 2
		/// - Primitives: 2 NOR gates = 6 NANDs
		/// - Feedback loops: 2 (cross-coupled)
		/// </summary>
		public static ChipDescription CreateSRLatch(string name)
		{
			var chip = new ChipDescription { Name = name };

			chip.InputPins = new[]
			{
				new PinDescription { ID = 0, Name = "S", BitCount = BitCount.One },
				new PinDescription { ID = 1, Name = "R", BitCount = BitCount.One }
			};

			chip.OutputPins = new[]
			{
				new PinDescription { ID = 0, Name = "Q", BitCount = BitCount.One },
				new PinDescription { ID = 1, Name = "Q_", BitCount = BitCount.One }
			};

			// SR latch uses two NOR gates cross-coupled
			// NOR = NOT(OR) = NOT(NOT(NAND(NOT(A), NOT(B))))
			// = NAND(NAND(A,A), NAND(B,B))

			var subChips = new List<SubChipDescription>();
			var wires = new List<WireDescription>();

			// First NOR gate (for Q output)
			int nor1_not_a = 0;  // NOT for input A
			int nor1_not_b = 1;  // NOT for input B
			int nor1_nand = 2;   // NAND of the two NOTs
			int nor1_not_out = 3; // Final NOT

			// Second NOR gate (for Q_ output)
			int nor2_not_a = 4;
			int nor2_not_b = 5;
			int nor2_nand = 6;
			int nor2_not_out = 7;

			for (int i = 0; i < 8; i++)
			{
				subChips.Add(new SubChipDescription { Name = "Nand", ID = i, Label = $"NAND{i}" });
			}

			// First NOR: inputs are S and Q_
			// NOT(S)
			wires.Add(CreateWire(-1, 1, nor1_not_a, 0)); // R -> NOT
			wires.Add(CreateWire(-1, 1, nor1_not_a, 1));

			// NOT(Q_) - feedback from second NOR output
			wires.Add(CreateWire(nor2_not_out, 0, nor1_not_b, 0));
			wires.Add(CreateWire(nor2_not_out, 0, nor1_not_b, 1));

			// NAND(NOT(R), NOT(Q_))
			wires.Add(CreateWire(nor1_not_a, 0, nor1_nand, 0));
			wires.Add(CreateWire(nor1_not_b, 0, nor1_nand, 1));

			// NOT(NAND(...)) = Q
			wires.Add(CreateWire(nor1_nand, 0, nor1_not_out, 0));
			wires.Add(CreateWire(nor1_nand, 0, nor1_not_out, 1));

			// Second NOR: inputs are R and Q
			// NOT(S)
			wires.Add(CreateWire(-1, 0, nor2_not_a, 0)); // S -> NOT
			wires.Add(CreateWire(-1, 0, nor2_not_a, 1));

			// NOT(Q) - feedback from first NOR output
			wires.Add(CreateWire(nor1_not_out, 0, nor2_not_b, 0));
			wires.Add(CreateWire(nor1_not_out, 0, nor2_not_b, 1));

			// NAND(NOT(S), NOT(Q))
			wires.Add(CreateWire(nor2_not_a, 0, nor2_nand, 0));
			wires.Add(CreateWire(nor2_not_b, 0, nor2_nand, 1));

			// NOT(NAND(...)) = Q_
			wires.Add(CreateWire(nor2_nand, 0, nor2_not_out, 0));
			wires.Add(CreateWire(nor2_nand, 0, nor2_not_out, 1));

			// Connect outputs
			wires.Add(CreateWire(nor1_not_out, 0, -1, 0)); // Q
			wires.Add(CreateWire(nor2_not_out, 0, -1, 1)); // Q_

			chip.SubChips = subChips.ToArray();
			chip.Wires = wires.ToArray();

			return chip;
		}

		/// <summary>
		/// Create a wide fanout circuit: one input drives many gates.
		///
		/// Properties:
		/// - Depth: 2
		/// - Width: fanout
		/// - Primitives: fanout
		/// - Feedback loops: 0
		/// </summary>
		public static ChipDescription CreateWideFanout(string name, int fanout)
		{
			var chip = new ChipDescription { Name = name };

			chip.InputPins = new[] { new PinDescription { ID = 0, Name = "In", BitCount = BitCount.One } };

			var outputs = new PinDescription[fanout];
			for (int i = 0; i < fanout; i++)
			{
				outputs[i] = new PinDescription { ID = i, Name = $"Out{i}", BitCount = BitCount.One };
			}
			chip.OutputPins = outputs;

			var subChips = new List<SubChipDescription>();
			var wires = new List<WireDescription>();

			// Create NOT gates, all driven by same input
			for (int i = 0; i < fanout; i++)
			{
				subChips.Add(new SubChipDescription { Name = "Nand", ID = i, Label = $"NOT{i}" });

				// Wire input to this NOT
				wires.Add(CreateWire(-1, 0, i, 0));
				wires.Add(CreateWire(-1, 0, i, 1));

				// Wire NOT output to corresponding output pin
				wires.Add(CreateWire(i, 0, -1, i));
			}

			chip.SubChips = subChips.ToArray();
			chip.Wires = wires.ToArray();

			return chip;
		}

		/// <summary>
		/// Helper to create a wire description.
		/// Chip ID -1 means the root chip (input/output dev pins).
		/// </summary>
		private static WireDescription CreateWire(int sourceChipID, int sourcePinID, int targetChipID, int targetPinID)
		{
			return new WireDescription
			{
				SourcePinAddress = new PinAddress { PinOwnerID = sourceChipID, PinID = sourcePinID },
				TargetPinAddress = new PinAddress { PinOwnerID = targetChipID, PinID = targetPinID },
				Points = Array.Empty<(float, float)>() // No visual routing needed for tests
			};
		}
	}
}
