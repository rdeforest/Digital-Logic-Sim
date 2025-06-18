using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class TriStateBufferChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.TriStateBuffer;

        protected override void ProcessChip(SimChip chip)
        {
            SimPin dataPin = chip.InputPins[0];
            SimPin enablePin = chip.InputPins[1];
            SimPin outputPin = chip.OutputPins[0];

            if (PinState.FirstBitHigh(enablePin.State))
            {
                outputPin.State = dataPin.State;
            }
            else
            {
                uint outputPinState = outputPin.State;
                PinState.SetAllDisconnected(ref outputPinState);
                outputPin.State = outputPinState;
            }
        }
    }
}
