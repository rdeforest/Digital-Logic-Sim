using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class NandChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Nand;

        protected override void ProcessChip(SimChip chip)
        {
            uint nandOp = 1 ^ (chip.InputPins[0].State & chip.InputPins[1].State);
            chip.OutputPins[0].State = (ushort)(nandOp & 1);
        }
    }
}
