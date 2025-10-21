using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
	public class DisplayLEDChipProcessor : BaseChipProcessor
	{
		public override ChipType ChipType => ChipType.DisplayLED;

		protected override void ProcessChip(SimChip chip)
		{
			// Display-only component - no processing needed
			// The visual representation reads input pin states directly
		}
	}
}
