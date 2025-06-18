using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class Rom256x16ChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Rom_256x16;

        protected override void ProcessChip(SimChip chip)
        {
            const int ByteMask = 0b11111111;
            uint address = PinState.GetBitStates(chip.InputPins[0].State);
            uint data = chip.InternalState[address];
            chip.OutputPins[0].State = (ushort)((data >> 8) & ByteMask);
            chip.OutputPins[1].State = (ushort)(data & ByteMask);
        }
    }
}
