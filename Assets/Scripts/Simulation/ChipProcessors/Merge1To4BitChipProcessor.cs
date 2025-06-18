using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class Merge1To4BitChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Merge_1To4Bit;

        protected override void ProcessChip(SimChip chip)
        {
            uint stateA = chip.InputPins[3].State & PinState.SingleBitMask; // lsb
            uint stateB = chip.InputPins[2].State & PinState.SingleBitMask;
            uint stateC = chip.InputPins[1].State & PinState.SingleBitMask;
            uint stateD = chip.InputPins[0].State & PinState.SingleBitMask;
            chip.OutputPins[0].State = stateA | stateB << 1 | stateC << 2 | stateD << 3;
        }
    }
}
