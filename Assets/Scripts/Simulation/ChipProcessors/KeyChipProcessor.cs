using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class KeyChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Key;

        protected override void ProcessChip(SimChip chip)
        {
            bool isHeld = SimKeyboardHelper.KeyIsHeld((char)chip.InternalState[0]);
            uint newState = isHeld ? PinState.LogicHigh : PinState.LogicLow;
            chip.OutputPins[0].State = newState;
            // Note: If state changed, the pin's State property setter will automatically add chip to dirty queue
        }
    }
}
