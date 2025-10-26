namespace DLS.Description
{
	public struct WireDescription
	{
		public PinAddress SourcePinAddress;
		public PinAddress TargetPinAddress;

		public WireDescription(PinAddress source, PinAddress target)
		{
			SourcePinAddress = source;
			TargetPinAddress = target;
		}
	}
}
