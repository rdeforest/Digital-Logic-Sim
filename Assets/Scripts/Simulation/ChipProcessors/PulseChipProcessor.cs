using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class PulseChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Pulse;

        protected override void ProcessChip(SimChip chip)
        {
            const int pulseDurationIndex = 0;
            const int pulseTicksRemainingIndex = 1;
            const int pulseInputOldIndex = 2;

            uint inputState = chip.InputPins[0].State;
            bool pulseInputHigh = PinState.FirstBitHigh(inputState);
            uint pulseTicksRemaining = chip.InternalState[pulseTicksRemainingIndex];

            if (pulseTicksRemaining == 0)
            {
                bool isRisingEdge = pulseInputHigh && chip.InternalState[pulseInputOldIndex] == 0;
                if (isRisingEdge)
                {
                    pulseTicksRemaining = chip.InternalState[pulseDurationIndex];
                    chip.InternalState[pulseTicksRemainingIndex] = pulseTicksRemaining;
                }
            }

            uint outputState = PinState.LogicLow;
            if (pulseTicksRemaining > 0)
            {
                chip.InternalState[1]--;
                outputState = PinState.LogicHigh;
            }
            else if (PinState.GetTristateFlags(inputState) != 0)
            {
                PinState.SetAllDisconnected(ref outputState);
            }

            chip.OutputPins[0].State = outputState;
            chip.InternalState[pulseInputOldIndex] = pulseInputHigh ? 1u : 0;
        }
    }
}
