using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class Split8To4BitChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Split_8To4Bit;

        protected override void ProcessChip(SimChip chip)
        {
            SimPin in8 = chip.InputPins[0];
            SimPin out4A = chip.OutputPins[0];
            SimPin out4B = chip.OutputPins[1];

            uint out4AState = out4A.State;
            uint out4BState = out4B.State;
            PinState.Set4BitFrom8BitSource(ref out4AState, in8.State, false);
            PinState.Set4BitFrom8BitSource(ref out4BState, in8.State, true);
            out4A.State = out4AState;
            out4B.State = out4BState;
        }
    }
}
