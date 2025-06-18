using DLS.Description;
using System.Linq;

namespace DLS.Simulation.ChipProcessors
{
    /// <summary>
    /// Processor for custom chips that handles subchip processing
    /// </summary>
    public class CustomChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.Custom;

        protected override void ProcessChip(SimChip chip)
        {
            // For custom chips, we only need to handle the dynamic reordering for race conditions.
            // The actual processing is handled by the dirty queue system - chips will be added
            // to the dirty queue when their inputs change or they have spontaneous state changes.
            
            // Every n frames (for performance reasons) the simulation permits some random modifications to the chip traversal order.
            // This is done to allow some variety in the outcomes of race-conditions (such as an SR latch having both inputs enabled, and then released).
            if (Simulator.canDynamicReorderThisFrame)
            {
                // NOTE: subchips are assumed to have been sorted in reverse order of desired visitation
                // We iterate backwards and use Skip(1) to avoid considering swaps for the first element
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
            
            // No need to call StepChip() on subchips - they will be processed via the dirty queue
            // when their inputs change or they have spontaneous state changes
        }
    }
}
