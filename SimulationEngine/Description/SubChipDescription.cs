namespace DLS.Description
{
	public struct SubChipDescription
	{
		public string Name; // Name of the chip type (e.g., "Nand", "AND", custom chip name)
		public int ID; // Unique ID within parent chip
		public string? Label; // Optional label for this instance
		public uint[]? InternalData; // For ROM/RAM chips

		public SubChipDescription(string name, int id, string? label = null, uint[]? internalData = null)
		{
			Name = name;
			ID = id;
			Label = label;
			InternalData = internalData;
		}
	}
}
