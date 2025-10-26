using System;

namespace DLS.Description
{
	public class ChipDescription
	{
		// ---- Name Comparison ----
		public const StringComparison NameComparison = StringComparison.OrdinalIgnoreCase;
		public static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

		// ---- Data ----
		public string Name = "";
		public ChipType ChipType;
		public PinDescription[] InputPins = Array.Empty<PinDescription>();
		public PinDescription[] OutputPins = Array.Empty<PinDescription>();
		public SubChipDescription[] SubChips = Array.Empty<SubChipDescription>();
		public WireDescription[] Wires = Array.Empty<WireDescription>();

		// ---- Convenience Functions ----
		public bool NameMatch(string otherName) => NameMatch(Name, otherName);
		public static bool NameMatch(string a, string b) => string.Equals(a, b, NameComparison);
	}
}
