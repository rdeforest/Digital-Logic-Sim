using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class Split8To1BitChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Split_8To1Bit;

        protected override void ProcessChip(SimChip chip)
        {
            uint in8 = chip.InputPins[0].State;
            chip.OutputPins[0].State = (in8 >> 7) & PinState.SingleBitMask;
            chip.OutputPins[1].State = (in8 >> 6) & PinState.SingleBitMask;
            chip.OutputPins[2].State = (in8 >> 5) & PinState.SingleBitMask;
            chip.OutputPins[3].State = (in8 >> 4) & PinState.SingleBitMask;
            chip.OutputPins[4].State = (in8 >> 3) & PinState.SingleBitMask;
            chip.OutputPins[5].State = (in8 >> 2) & PinState.SingleBitMask;
            chip.OutputPins[6].State = (in8 >> 1) & PinState.SingleBitMask;
            chip.OutputPins[7].State = (in8 >> 0) & PinState.SingleBitMask;
        }
    }
}
