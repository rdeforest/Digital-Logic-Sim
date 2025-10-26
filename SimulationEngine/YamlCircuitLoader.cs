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
	/// Loads test configurations from YAML files.
	/// References existing DLS projects and circuits.
	///
	/// Example YAML format:
	/// ---
	/// name: 4x4 Multiplier Test
	/// project: z80
	/// circuit: 4x4 mult
	/// max_cycles_per_test: 100
	/// test_vectors:
	///   - inputs: {A: 3, B: 4}
	///     expected: {OUT: 12}
	/// </summary>
	public class YamlCircuitLoader
	{
		public class TestSpec
		{
			public string Name { get; set; } = "";
			public string? Project { get; set; }  // Project name (loads from TestData/Projects/{name}/)
			public string? Circuit { get; set; }  // Circuit name within project
			public int MaxCyclesPerTest { get; set; } = 100;
			public List<TestVector>? TestVectors { get; set; }
		}

		public class TestVector
		{
			public Dictionary<string, int>? Inputs { get; set; }
			public Dictionary<string, int>? Expected { get; set; }
		}

		// Legacy format support (for backwards compatibility with simple YAML circuits)
		public class CircuitSpec
		{
			public string Name { get; set; } = "";
			public int Cycles { get; set; } = 1000;
			public CircuitDefinition? Circuit { get; set; }
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

		public static TestSpec LoadTestFromFile(string yamlFilePath)
		{
			var yaml = File.ReadAllText(yamlFilePath);
			return LoadTestFromString(yaml);
		}

		public static TestSpec LoadTestFromString(string yaml)
		{
			var deserializer = new DeserializerBuilder()
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			return deserializer.Deserialize<TestSpec>(yaml);
		}

		/// <summary>
		/// Load a chip from a DLS project.
		/// Looks in TestData/Projects/{projectName}/Chips/{chipName}.json
		/// </summary>
		public static ChipDescription LoadChipFromProject(string projectName, string chipName, string? testDataPath = null)
		{
			// Default to TestData in repository root
			testDataPath ??= Path.Combine(
				Directory.GetCurrentDirectory(),
				"..", "..", "..", "..", // Navigate up from bin/Debug/net8.0/
				"TestData", "Projects"
			);

			string chipPath = Path.Combine(testDataPath, projectName, "Chips", $"{chipName}.json");

			if (!File.Exists(chipPath))
			{
				throw new FileNotFoundException($"Chip not found: {chipPath}");
			}

			string json = File.ReadAllText(chipPath);

			// Use the existing DLS serializer
			var chip = Newtonsoft.Json.JsonConvert.DeserializeObject<ChipDescription>(json);

			if (chip == null)
			{
				throw new Exception($"Failed to deserialize chip: {chipName}");
			}

			return chip;
		}

		// Legacy support for old YAML format
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

			if (spec.Circuit == null)
			{
				throw new Exception("Legacy YAML format requires 'circuit' section");
			}

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
