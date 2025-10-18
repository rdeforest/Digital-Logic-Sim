using DLS.Description;
using System.Linq;

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
        /// Override StepChip to make custom chips transparent.
        /// Custom chips propagate signals through without adding edge delays -
        /// subchips are processed in the same wave rather than waiting for the next wave.
        /// </summary>
        public override void StepChip(SimChip chip)
        {
            // Propagate inputs to connected internal pins
            chip.Sim_PropagateInputs();

            // For custom chips to be transparent (zero-delay), we need to move any
            // subchips that were added to nextWave into currentWave so they process
            // in the same breadth-first wave
            foreach (SimChip subChip in chip.SubChips)
            {
                if (Simulator.IsInNextWave(subChip))
                {
                    Simulator.RemoveFromNextWave(subChip);
                    Simulator.AddToCurrentWave(subChip);
                }
            }

            // Don't call ProcessChip() - custom chips have no logic of their own
            // The subchips will be processed in the current wave

            // Propagate outputs
            chip.Sim_PropagateOutputs();
        }

        protected override void ProcessChip(SimChip chip)
        {
            // Custom chips have no processing logic of their own
            // Dynamic reordering is handled elsewhere if needed
            if (Simulator.canDynamicReorderThisFrame)
            {
                foreach (var (currentChip, index) in chip.SubChips.Reverse().Select((c, i) => (c, chip.SubChips.Length - 1 - i)).Skip(1))
                {
                    if (!currentChip.Sim_IsReady() && Simulator.RandomBool())
                    {
                        SimChip potentialSwapChip = chip.SubChips[index - 1];
                        if (!ChipTypeHelper.IsBusOriginType(potentialSwapChip.ChipType))
                        {
                            (chip.SubChips[index], chip.SubChips[index - 1]) = (chip.SubChips[index - 1], chip.SubChips[index]);
                        }
                    }
                }
            }
        }
    }
}
