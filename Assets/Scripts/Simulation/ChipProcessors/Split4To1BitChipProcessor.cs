using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class Split4To1BitChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Split_4To1Bit;

        protected override void ProcessChip(SimChip chip)
        {
            uint inState4Bit = chip.InputPins[0].State;
            chip.OutputPins[0].State = (inState4Bit >> 3) & PinState.SingleBitMask;
            chip.OutputPins[1].State = (inState4Bit >> 2) & PinState.SingleBitMask;
            chip.OutputPins[2].State = (inState4Bit >> 1) & PinState.SingleBitMask;
            chip.OutputPins[3].State = (inState4Bit >> 0) & PinState.SingleBitMask;
        }
    }
}
