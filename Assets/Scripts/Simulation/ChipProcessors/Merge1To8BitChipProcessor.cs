using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class Merge1To8BitChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Merge_1To8Bit;

        protected override void ProcessChip(SimChip chip)
        {
            uint stateA = chip.InputPins[7].State & PinState.SingleBitMask; // lsb
            uint stateB = chip.InputPins[6].State & PinState.SingleBitMask;
            uint stateC = chip.InputPins[5].State & PinState.SingleBitMask;
            uint stateD = chip.InputPins[4].State & PinState.SingleBitMask;
            uint stateE = chip.InputPins[3].State & PinState.SingleBitMask;
            uint stateF = chip.InputPins[2].State & PinState.SingleBitMask;
            uint stateG = chip.InputPins[1].State & PinState.SingleBitMask;
            uint stateH = chip.InputPins[0].State & PinState.SingleBitMask;
            chip.OutputPins[0].State = stateA | stateB << 1 | stateC << 2 | stateD << 3 | stateE << 4 | stateF << 5 | stateG << 6 | stateH << 7;
        }
    }
}
