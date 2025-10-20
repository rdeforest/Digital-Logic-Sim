using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    /// <summary>
    /// Processor for custom chips that handles subchip processing.
    /// Custom chips are transparent organizational units - they propagate signals
    /// through to their subchips without adding edge delays.
    /// </summary>
    public class CustomChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Custom;

        /// <summary>
        /// Custom chips should never be in waves - they're containers, not processors.
        /// If this is called, something is wrong with the architecture.
        /// </summary>
        public override void StepChip(SimChip chip)
        {
            throw new System.Exception("CustomChipProcessor.StepChip() should never be called. Custom chips are containers and should be expanded, not processed.");
        }

        protected override void ProcessChip(SimChip chip)
        {
            // Custom chips have no processing logic of their own
            // Wave-based processing handles all ordering automatically
        }
    }
}
