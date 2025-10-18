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
