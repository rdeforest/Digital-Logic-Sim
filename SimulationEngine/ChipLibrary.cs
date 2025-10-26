using System;
using System.Collections.Generic;
using DLS.Description;

namespace DLS.Simulation
{
	/// <summary>
	/// Minimal chip library for standalone simulation.
	/// Contains only built-in chip definitions needed for simulation.
	/// </summary>
	public class ChipLibrary
	{
		readonly Dictionary<string, ChipDescription> chips = new(ChipDescription.NameComparer);

		public ChipLibrary()
		{
			// Add minimal built-in chips
			AddBuiltinChip(CreateNandChip());
			// Can add more as needed
		}

		public void AddChip(ChipDescription chip)
		{
			chips[chip.Name] = chip;
		}

		public ChipDescription GetChipDescription(string name)
		{
			if (chips.TryGetValue(name, out var chip))
			{
				return chip;
			}
			throw new Exception($"Chip not found: {name}");
		}

		void AddBuiltinChip(ChipDescription chip)
		{
			chips[chip.Name] = chip;
		}

		static ChipDescription CreateNandChip()
		{
			return new ChipDescription
			{
				Name = "Nand",
				ChipType = ChipType.Nand,
				InputPins = new[]
				{
					new PinDescription("A", 0, BitCount.One),
					new PinDescription("B", 1, BitCount.One)
				},
				OutputPins = new[]
				{
					new PinDescription("Out", 0, BitCount.One)
				}
			};
		}
	}
}
