using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class Merge4To8BitChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Merge_4To8Bit;

        protected override void ProcessChip(SimChip chip)
        {
            SimPin in4A = chip.InputPins[0];
            SimPin in4B = chip.InputPins[1];
            SimPin out8 = chip.OutputPins[0];

            uint out8State = out8.State;
            PinState.Set8BitFrom4BitSources(ref out8State, in4B.State, in4A.State);
            out8.State = out8State;
        }
    }
}
