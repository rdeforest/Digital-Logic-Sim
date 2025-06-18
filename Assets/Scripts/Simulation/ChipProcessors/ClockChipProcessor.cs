using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class ClockChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Clock;

        protected override void ProcessChip(SimChip chip)
        {
            bool high = Simulator.stepsPerClockTransition != 0 && ((Simulator.simulationFrame / Simulator.stepsPerClockTransition) & 1) == 0;
            uint newPinState = chip.OutputPins[0].State;
            PinState.Set(ref newPinState, high ? PinState.LogicHigh : PinState.LogicLow);
            chip.OutputPins[0].State = newPinState;
            // Note: If state changed, the pin's State property setter will automatically add chip to dirty queue
        }
    }
}
