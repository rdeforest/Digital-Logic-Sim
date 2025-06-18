using DLS.Description;
using System.Linq;

namespace DLS.Simulation.ChipProcessors
{
    /// <summary>
    /// Base class for all chip processors. Each chip type should inherit from this
    /// and implement the ProcessChip method to define their specific behavior.
    /// </summary>
    public abstract class BaseChipProcessor
    {
        /// <summary>
        /// Main entry point for chip processing. Handles common chip processing workflow
        /// and delegates chip-specific logic to ProcessChip method.
        /// </summary>
        /// <param name="chip">The SimChip instance to process</param>
        public virtual void StepChip(SimChip chip)
        {
            // Propagate signal from all input dev-pins to all their connected pins
            chip.Sim_PropagateInputs();

            // Call the chip-specific processing logic (works for both builtin and custom chips)
            ProcessChip(chip);

            // Propagate outputs to connected pins
            chip.Sim_PropagateOutputs();
        }

        /// <summary>
        /// Process the chip-specific logic for builtin chips.
        /// This method should read from InputPins, perform the chip's logic,
        /// and write to OutputPins and InternalState as needed.
        /// </summary>
        /// <param name="chip">The SimChip instance to process</param>
        protected abstract void ProcessChip(SimChip chip);

        /// <summary>
        /// Process the chip-specific logic with audio state for chips that need audio functionality.
        /// Override this method if your chip processor needs access to simulation context.
        /// </summary>
        /// <param name="chip">The SimChip instance to process</param>
        /// <param name="audioState">The audio state for chips that need audio functionality</param>
        protected virtual void ProcessChip(SimChip chip, SimAudio audioState)
        {
            ProcessChip(chip); // Default implementation just calls the basic version
        }

        /// <summary>
        /// Get the chip type that this processor handles
        /// </summary>
        public abstract ChipType ChipType { get; }
    }
}
