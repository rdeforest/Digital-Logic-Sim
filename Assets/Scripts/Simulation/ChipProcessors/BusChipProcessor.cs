using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class BusChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Bus_1Bit; // This will handle all bus types

        protected override void ProcessChip(SimChip chip)
        {
            // All bus types have the same behavior: copy input to output
            if (ChipTypeHelper.IsBusOriginType(chip.ChipType))
            {
                SimPin inputPin = chip.InputPins[0];
                uint outputPinState = chip.OutputPins[0].State;
                PinState.Set(ref outputPinState, inputPin.State);
                chip.OutputPins[0].State = outputPinState;
            }
        }
    }
}
