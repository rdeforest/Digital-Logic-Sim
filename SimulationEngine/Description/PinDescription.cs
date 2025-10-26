namespace DLS.Description
{
	public struct PinDescription
	{
		public string Name;
		public int ID;
		public BitCount BitCount;

		public PinDescription(string name, int id, BitCount bitCount)
		{
			Name = name;
			ID = id;
			BitCount = bitCount;
		}
	}

	public enum BitCount
	{
		One = 1,
		Four = 4,
		Eight = 8
	}

	public struct PinAddress
	{
		public int PinOwnerID; // -1 for chip itself, >= 0 for subchip ID
		public int PinID;

		public PinAddress(int pinOwnerID, int pinID)
		{
			PinOwnerID = pinOwnerID;
			PinID = pinID;
		}
	}
}
