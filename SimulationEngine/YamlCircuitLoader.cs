using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLS.Description;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DLS.Simulation
{
	/// <summary>
	/// Loads circuit definitions from YAML files.
	///
	/// Example YAML format:
	/// ---
	/// name: single NAND loop
	/// cycles: 1000
	/// circuit:
	///   inputs:
	///     - in0: 1bit
	///   outputs:
	///     - out0: 1bit
	///   parts:
	///     - nand0:
	///         type: nand
	///   wires:
	///     - from: nand0.out0
	///       to: [nand0.in0, nand0.in1]
	/// </summary>
	public class YamlCircuitLoader
	{
		public class CircuitSpec
		{
			public string Name { get; set; } = "";
			public int Cycles { get; set; } = 1000;
			public CircuitDefinition Circuit { get; set; } = new();
		}

		public class CircuitDefinition
		{
			public List<Dictionary<string, string>>? Inputs { get; set; }
			public List<Dictionary<string, string>>? Outputs { get; set; }
			public List<Dictionary<string, PartDefinition>>? Parts { get; set; }
			public List<WireDefinition>? Wires { get; set; }
		}

		public class PartDefinition
		{
			public string Type { get; set; } = "";
		}

		public class WireDefinition
		{
			public string From { get; set; } = "";
			public object? To { get; set; } // Can be string or List<string>
		}

		public static (ChipDescription chip, int cycles) LoadFromFile(string yamlFilePath)
		{
			var yaml = File.ReadAllText(yamlFilePath);
			return LoadFromString(yaml);
		}

		public static (ChipDescription chip, int cycles) LoadFromString(string yaml)
		{
			var deserializer = new DeserializerBuilder()
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			var spec = deserializer.Deserialize<CircuitSpec>(yaml);
			var chip = BuildChipDescription(spec);
			return (chip, spec.Cycles);
		}

		static ChipDescription BuildChipDescription(CircuitSpec spec)
		{
			var chip = new ChipDescription
			{
				Name = spec.Name,
				ChipType = ChipType.Custom
			};

			// Parse inputs
			var inputPins = new List<PinDescription>();
			if (spec.Circuit.Inputs != null)
			{
				int pinId = 0;
				foreach (var inputDef in spec.Circuit.Inputs)
				{
					foreach (var kvp in inputDef)
					{
						inputPins.Add(new PinDescription(
							kvp.Key,
							pinId++,
							ParseBitCount(kvp.Value)
						));
					}
				}
			}
			chip.InputPins = inputPins.ToArray();

			// Parse outputs
			var outputPins = new List<PinDescription>();
			if (spec.Circuit.Outputs != null)
			{
				int pinId = 0;
				foreach (var outputDef in spec.Circuit.Outputs)
				{
					foreach (var kvp in outputDef)
					{
						outputPins.Add(new PinDescription(
							kvp.Key,
							pinId++,
							ParseBitCount(kvp.Value)
						));
					}
				}
			}
			chip.OutputPins = outputPins.ToArray();

			// Parse parts (subchips)
			var subChips = new List<SubChipDescription>();
			var partNameToId = new Dictionary<string, int>();

			if (spec.Circuit.Parts != null)
			{
				int chipId = 0;
				foreach (var partDef in spec.Circuit.Parts)
				{
					foreach (var kvp in partDef)
					{
						string partName = kvp.Key;
						string chipType = kvp.Value.Type;

						partNameToId[partName] = chipId;
						subChips.Add(new SubChipDescription(
							chipType,
							chipId,
							partName
						));
						chipId++;
					}
				}
			}
			chip.SubChips = subChips.ToArray();

			// Parse wires
			var wires = new List<WireDescription>();
			if (spec.Circuit.Wires != null)
			{
				foreach (var wireDef in spec.Circuit.Wires)
				{
					var source = ParsePinAddress(wireDef.From, partNameToId);

					// Handle both single target and multiple targets
					var targets = new List<string>();
					if (wireDef.To is string singleTarget)
					{
						targets.Add(singleTarget);
					}
					else if (wireDef.To is List<object> multipleTargets)
					{
						targets.AddRange(multipleTargets.Select(t => t.ToString() ?? ""));
					}

					foreach (var targetStr in targets)
					{
						var target = ParsePinAddress(targetStr, partNameToId);
						wires.Add(new WireDescription(source, target));
					}
				}
			}
			chip.Wires = wires.ToArray();

			return chip;
		}

		static BitCount ParseBitCount(string bitCountStr)
		{
			return bitCountStr.ToLowerInvariant() switch
			{
				"1bit" or "1" => BitCount.One,
				"4bit" or "4" => BitCount.Four,
				"8bit" or "8" => BitCount.Eight,
				_ => throw new Exception($"Unknown bit count: {bitCountStr}")
			};
		}

		static PinAddress ParsePinAddress(string pinAddr, Dictionary<string, int> partNameToId)
		{
			// Format: "partName.pinName" or "pinName" for chip-level pins
			var parts = pinAddr.Split('.');

			if (parts.Length == 1)
			{
				// Chip-level pin (input/output)
				// For simplicity, use -1 as chip owner ID and parse pin name
				// Note: This is simplified - real implementation would need pin name -> ID mapping
				return new PinAddress(-1, 0);
			}
			else if (parts.Length == 2)
			{
				string partName = parts[0];
				string pinName = parts[1];

				if (!partNameToId.TryGetValue(partName, out int chipId))
				{
					throw new Exception($"Unknown part: {partName}");
				}

				// Parse pin name (e.g., "in0", "in1", "out0")
				int pinId = ParsePinName(pinName);

				return new PinAddress(chipId, pinId);
			}
			else
			{
				throw new Exception($"Invalid pin address format: {pinAddr}");
			}
		}

		static int ParsePinName(string pinName)
		{
			// Extract number from pin name (e.g., "in0" -> 0, "out1" -> 1)
			// For NAND gates: in0, in1, out0
			if (pinName.StartsWith("in"))
			{
				return int.Parse(pinName.Substring(2));
			}
			else if (pinName.StartsWith("out"))
			{
				return int.Parse(pinName.Substring(3));
			}
			else
			{
				throw new Exception($"Invalid pin name format: {pinName}");
			}
		}
	}
}
