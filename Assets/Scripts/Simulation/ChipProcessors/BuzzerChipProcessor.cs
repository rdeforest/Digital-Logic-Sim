using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class BuzzerChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Buzzer;

        protected override void ProcessChip(SimChip chip)
        {
            // This version is called when no audio state is available
            // The audio functionality will be handled by the overloaded version
        }

        protected override void ProcessChip(SimChip chip, SimAudio audioState)
        {
            int freqIndex = PinState.GetBitStates(chip.InputPins[0].State);
            int volumeIndex = PinState.GetBitStates(chip.InputPins[1].State);
            audioState.RegisterNote(freqIndex, (uint)volumeIndex);
        }
    }
}
