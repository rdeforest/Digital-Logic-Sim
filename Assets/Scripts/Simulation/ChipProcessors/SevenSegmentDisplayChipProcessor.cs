using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
	public class SevenSegmentDisplayChipProcessor : BaseChipProcessor
	{
		public override ChipType ChipType => ChipType.SevenSegmentDisplay;

		protected override void ProcessChip(SimChip chip)
		{
			// Display-only component - no processing needed
			// The visual representation reads input pin states directly
		}
	}
}
