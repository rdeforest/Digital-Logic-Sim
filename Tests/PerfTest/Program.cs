using System;
using System.Diagnostics;
using System.IO;
using DLS.Simulation;
using DLS.Description;

namespace DLS.PerfTest
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				PrintUsage();
				return;
			}

			string yamlFile = args[0];
			string mode = args.Length > 1 && args[1] == "--mode" && args.Length > 2 ? args[2] : "compare";
			bool compare = mode == "compare";

			if (!File.Exists(yamlFile))
			{
				Console.WriteLine($"Error: File not found: {yamlFile}");
				return;
			}

			try
			{
				// Load circuit from YAML
				var (chip, cycles) = YamlCircuitLoader.LoadFromFile(yamlFile);
				Console.WriteLine($"=== Loaded Circuit: {chip.Name} ===");
				Console.WriteLine($"Primitives: {CountPrimitives(chip)}");
				Console.WriteLine($"Cycles to simulate: {cycles}");
				Console.WriteLine();

				if (compare || mode == "dfs")
				{
					RunTest(chip, cycles, SimulationMode.DepthFirst);
				}

				if (compare || mode == "bfs")
				{
					RunTest(chip, cycles, SimulationMode.BreadthFirst);
				}

				if (compare)
				{
					Console.WriteLine("\n=== Comparison ===");
					Console.WriteLine("Run both modes above to compare results");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				Console.WriteLine(ex.StackTrace);
			}
		}

		static void RunTest(ChipDescription chipDesc, int cycles, SimulationMode mode)
		{
			string modeName = mode == SimulationMode.DepthFirst ? "DFS" : "BFS";
			Console.WriteLine($"=== Testing {modeName} ===");

			// Build simulation
			var rootChip = SimulatorStandalone.BuildSimChip(chipDesc);

			// Configure simulation
			Simulator.CurrentMode = mode;
			Simulator.EnableMetrics = true;
			Simulator.Metrics.Reset();
			Simulator.Metrics.TotalPrimitives = CountPrimitives(chipDesc);

			// Warm up (JIT compilation, etc.)
			for (int i = 0; i < 100; i++)
			{
				SimulatorStandalone.RunSimulationStep(rootChip);
			}

			// Reset after warmup
			Simulator.Metrics.Reset();
			Simulator.simulationFrame = 0;

			// Run actual test with timing
			var sw = Stopwatch.StartNew();
			long methodCalls = 0; // TODO: Instrument for actual method call counting

			for (int i = 0; i < cycles; i++)
			{
				SimulatorStandalone.RunSimulationStep(rootChip);
			}

			sw.Stop();

			// Output results
			Console.WriteLine("\nLogical Work:");
			Console.WriteLine($"  Primitive Evaluations: {Simulator.Metrics.PrimitiveEvaluations:N0}");
			Console.WriteLine($"  Pin State Changes:     {Simulator.Metrics.PinStateChanges:N0}");
			Console.WriteLine($"  Wasted Evaluations:    {Simulator.Metrics.WastedEvaluations:N0}");
			Console.WriteLine($"  Evaluation Efficiency: {Simulator.Metrics.EvaluationEfficiency:P2}");

			Console.WriteLine("\nAlgorithm Overhead:");
			Console.WriteLine($"  Resort Operations:     {Simulator.Metrics.ResortOperations:N0}");
			Console.WriteLine($"  Reorder Attempts:      {Simulator.Metrics.DynamicReorderAttempts:N0}");
			Console.WriteLine($"  Ready Checks:          {Simulator.Metrics.ReadyChecks:N0}");
			Console.WriteLine($"  Max Stack Depth:       {Simulator.Metrics.MaxCallStackDepth}");

			Console.WriteLine("\nCPU Performance:");
			Console.WriteLine($"  Wall Time:             {sw.Elapsed.TotalMilliseconds:F3} ms");
			Console.WriteLine($"  Cycles/Second:         {cycles / sw.Elapsed.TotalSeconds:N0}");
			Console.WriteLine($"  Avg Time/Cycle:        {sw.Elapsed.TotalMilliseconds / cycles:F6} ms");

			if (Simulator.Metrics.PrimitiveEvaluations > 0)
			{
				Console.WriteLine($"  Evals/Second:          {Simulator.Metrics.PrimitiveEvaluations / sw.Elapsed.TotalSeconds:N0}");
			}

			Console.WriteLine();
		}

		static int CountPrimitives(ChipDescription chip)
		{
			int count = 0;
			foreach (var subChip in chip.SubChips)
			{
				// For now, assume all subchips are primitives
				// In full implementation, would need to recursively count
				count++;
			}
			return count;
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: perf-test <circuit.yaml> [options]");
			Console.WriteLine();
			Console.WriteLine("Options:");
			Console.WriteLine("  --mode dfs      Run only Depth-First Search algorithm");
			Console.WriteLine("  --mode bfs      Run only Breadth-First Search algorithm");
			Console.WriteLine("  --mode compare  Run both and compare (default)");
			Console.WriteLine();
			Console.WriteLine("Examples:");
			Console.WriteLine("  perf-test tests/circuits/nand-loop.yaml");
			Console.WriteLine("  perf-test tests/circuits/not-chain-10.yaml --mode dfs");
		}
	}
}
