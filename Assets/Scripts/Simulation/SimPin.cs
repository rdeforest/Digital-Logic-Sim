using System;
using NUnit.Framework;

namespace DLS.Simulation
{
	public class SimPin
	{
		public readonly int ID;
		public readonly SimChip parentChip;
		public readonly bool isInput;
		
		private uint _state;
		public uint State
		{
			get => _state;
			set
			{
				if (_state != value)
				{
					_state = value;

					// Output pins always propagate their changes to connected inputs
					if (!isInput)
					{
						PropagateSignal();
					}
					// Input pins are handled by NotifyChipIfReady() for transparent chips
					// Wave-processing chips no longer need dirty marking - topological sort handles execution order
				}
			}
		}

		public SimPin[] ConnectedTargetPins = Array.Empty<SimPin>();

		// Simulation frame index on which pin last received an input
		public int lastUpdatedFrameIndex;

		// Address of pin from where this pin last received its input
		public int latestSourceID;
		public int latestSourceParentChipID;

		// Number of wires that input their signal to this pin.
		// (In the case of conflicting signals, the pin chooses randomly)
		public int numInputConnections;
		public int numInputsReceivedThisFrame;

		public SimPin(int id, bool isInput, SimChip parentChip)
		{
			this.parentChip = parentChip;
			this.isInput = isInput;
			ID = id;
			latestSourceID = -1;
			latestSourceParentChipID = -1;

			uint tmpState = State;
			PinState.SetAllDisconnected(ref tmpState);
			State = tmpState;
		}

		public bool FirstBitHigh => PinState.FirstBitHigh(State);

		public void PropagateSignal()
		{
			foreach (SimPin targetPin in ConnectedTargetPins)
			{
				targetPin.ReceiveInput(this);
			}
		}

		// Called on sub-chip input pins, or chip dev-pins
		void ReceiveInput(SimPin source)
		{
			InitializeFrameIfNeeded();
			
			var set = ResolveInputState(source);
			
			HandleStateChange(source, set);
			
			numInputsReceivedThisFrame++;
			NotifyChipIfReady();
		}

		private void InitializeFrameIfNeeded()
		{
			if (lastUpdatedFrameIndex != Simulator.simulationFrame)
			{
				lastUpdatedFrameIndex = Simulator.simulationFrame;
				numInputsReceivedThisFrame = 0;
			}
		}

		private bool ResolveInputState(SimPin source)
		{
			if (numInputsReceivedThisFrame > 0)
			{
				return ResolveConflictingInput(source);
			}
			else
			{
				return AcceptFirstInput(source);
			}
		}

		private bool ResolveConflictingInput(SimPin source)
		{
			// Has already received input this frame, so choose at random whether to accept conflicting input.
			// Note: for multi-bit pins, this choice is made identically for all bits, rather than individually.
			uint OR = source.State | State;
			uint AND = source.State & State;
			ushort bitsNew = (ushort)(Simulator.RandomBool() ? OR : AND); // randomly accept or reject conflicting state

			ushort mask = (ushort)(OR >> 16); // tristate flags
			bitsNew = (ushort)((bitsNew & ~mask) | ((ushort)OR & mask)); // can always accept input for tristated bits

			ushort tristateNew = (ushort)(AND >> 16);
			uint stateNew = (uint)(bitsNew | (tristateNew << 16));
			bool changed = stateNew != State;
			State = stateNew;
			return changed;
		}

		private bool AcceptFirstInput(SimPin source)
		{
			// First input source this frame, so accept it.
			bool changed = State != source.State;
			State = source.State;
			return changed;
		}

		private void HandleStateChange(SimPin source, bool set)
		{
			if (set)
			{
				latestSourceID = source.ID;
				latestSourceParentChipID = source.parentChip.ID;
			}
		}

		private void NotifyChipIfReady()
		{
			// If this is a sub-chip input pin, and has received all of its connections, trigger processing
			if (isInput && numInputsReceivedThisFrame == numInputConnections)
			{
				// Custom chips are containers - propagate their inputs through to internal primitives
				if (parentChip.ChipType == DLS.Description.ChipType.Custom)
				{
					parentChip.Sim_PropagateInputs();
					return;
				}

				// For transparent components, process immediately when all inputs received
				if (!DLS.Description.ChipTypeHelper.RequiresWaveProcessing(parentChip.ChipType))
				{
					parentChip.StepChip();
				}
			}
		}
	}
}
