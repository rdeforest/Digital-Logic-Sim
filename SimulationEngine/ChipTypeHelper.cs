using System;

namespace DLS.Description
{
	public static class ChipTypeHelper
	{
		public static bool IsBusOriginType(ChipType type) =>
			type is ChipType.Bus_1Bit or ChipType.Bus_4Bit or ChipType.Bus_8Bit;

		public static bool IsDevPin(ChipType type) =>
			type is ChipType.In_1Bit or ChipType.In_4Bit or ChipType.In_8Bit or
			       ChipType.Out_1Bit or ChipType.Out_4Bit or ChipType.Out_8Bit;
	}
}
