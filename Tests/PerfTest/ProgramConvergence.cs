using System;
using System.Linq;
using DLS.Description;
using DLS.Simulation;

namespace DLS.PerfTest
{
	partial class Program
	{
		static void RunConvergenceTests(YamlCircuitLoader.TestSpec testSpec, bool compare, string mode)
		{
			Console.WriteLine($"=== {testSpec.Name} ===");
			Console.WriteLine($"Project: {testSpec.Project}");
			Console.WriteLine($"Circuit: {testSpec.Circuit}");
			Console.WriteLine($"Max Cycles Per Test: {testSpec.MaxCyclesPerTest}");
			Console.WriteLine($"Test Vectors: {testSpec.TestVectors?.Count ?? 0}");
			Console.WriteLine();

			// Load chip from project
			var chip = YamlCircuitLoader.LoadChipFromProject(testSpec.Project!, testSpec.Circuit!);
			Console.WriteLine($"Loaded circuit with {CountPrimitives(chip)} primitives");
			Console.WriteLine();

			if (testSpec.TestVectors == null || testSpec.TestVectors.Count == 0)
			{
				Console.WriteLine("No test vectors defined!");
				return;
			}

			// Run tests for each algorithm
			if (compare || mode == "dfs")
			{
				Console.WriteLine("=== DFS Results ===");
				RunConvergenceTestsForMode(chip, testSpec, SimulationMode.DepthFirst);
				Console.WriteLine();
			}

			if (compare || mode == "bfs")
			{
				Console.WriteLine("=== BFS Results ===");
				RunConvergenceTestsForMode(chip, testSpec, SimulationMode.BreadthFirst);
				Console.WriteLine();
			}

			if (compare)
			{
				Console.WriteLine("=== Comparison ===");
				CompareResults(chip, testSpec);
			}
		}

		static void RunConvergenceTestsForMode(
			ChipDescription chip,
			YamlCircuitLoader.TestSpec testSpec,
			SimulationMode mode)
		{
			int testNum = 0;
			long totalCycles = 0;
			long totalEvals = 0;
			int convergedCount = 0;

			foreach (var testVector in testSpec.TestVectors!)
			{
				testNum++;

				// Format inputs for display
				string inputsStr = string.Join(", ",
					testVector.Inputs!.Select(kvp => $"{kvp.Key}={kvp.Value}"));
				string expectedStr = string.Join(", ",
					testVector.Expected!.Select(kvp => $"{kvp.Key}={kvp.Value}"));

				var result = ConvergenceTest.RunTestVector(
					chip,
					testVector.Inputs!,
					testVector.Expected!,
					testSpec.MaxCyclesPerTest,
					mode);

				totalCycles += result.CyclesToConverge;
				totalEvals += result.PrimitiveEvaluations;
				if (result.Converged) convergedCount++;

				// Print result
				Console.WriteLine($"Test {testNum}: {inputsStr} → {expectedStr}");
				if (result.Converged)
				{
					Console.WriteLine($"  ✓ Converged in {result.CyclesToConverge} cycles");
					Console.WriteLine($"  Evaluations: {result.PrimitiveEvaluations:N0}, Wall Time: {result.WallTimeMs:F3} ms");
				}
				else
				{
					Console.WriteLine($"  ✗ Failed to converge (timeout at {result.CyclesToConverge} cycles)");
					Console.WriteLine($"  {result.FailureReason}");
				}
			}

			// Summary
			Console.WriteLine($"\nSummary:");
			Console.WriteLine($"  Tests passed: {convergedCount}/{testSpec.TestVectors.Count}");
			Console.WriteLine($"  Avg cycles to converge: {totalCycles / (double)testSpec.TestVectors.Count:F1}");
			Console.WriteLine($"  Total evaluations: {totalEvals:N0}");
		}

		static void CompareResults(ChipDescription chip, YamlCircuitLoader.TestSpec testSpec)
		{
			Console.WriteLine("Running side-by-side comparison...\n");

			int testNum = 0;
			int dfsWins = 0;
			int bfsWins = 0;
			int ties = 0;

			foreach (var testVector in testSpec.TestVectors!)
			{
				testNum++;

				var dfsResult = ConvergenceTest.RunTestVector(
					chip,
					testVector.Inputs!,
					testVector.Expected!,
					testSpec.MaxCyclesPerTest,
					SimulationMode.DepthFirst);

				var bfsResult = ConvergenceTest.RunTestVector(
					chip,
					testVector.Inputs!,
					testVector.Expected!,
					testSpec.MaxCyclesPerTest,
					SimulationMode.BreadthFirst);

				string inputsStr = string.Join(", ",
					testVector.Inputs!.Select(kvp => $"{kvp.Key}={kvp.Value}"));

				Console.WriteLine($"Test {testNum}: {inputsStr}");
				Console.WriteLine($"  DFS: {(dfsResult.Converged ? $"{dfsResult.CyclesToConverge} cycles" : "TIMEOUT")}");
				Console.WriteLine($"  BFS: {(bfsResult.Converged ? $"{bfsResult.CyclesToConverge} cycles" : "TIMEOUT")}");

				if (dfsResult.Converged && bfsResult.Converged)
				{
					if (dfsResult.CyclesToConverge < bfsResult.CyclesToConverge)
					{
						Console.WriteLine($"  → DFS wins by {bfsResult.CyclesToConverge - dfsResult.CyclesToConverge} cycles");
						dfsWins++;
					}
					else if (bfsResult.CyclesToConverge < dfsResult.CyclesToConverge)
					{
						Console.WriteLine($"  → BFS wins by {dfsResult.CyclesToConverge - bfsResult.CyclesToConverge} cycles");
						bfsWins++;
					}
					else
					{
						Console.WriteLine($"  → Tie (both {dfsResult.CyclesToConverge} cycles)");
						ties++;
					}
				}
				else
				{
					Console.WriteLine($"  → Both failed or one failed");
				}

				Console.WriteLine();
			}

			// Overall winner
			Console.WriteLine("=== Overall Results ===");
			Console.WriteLine($"DFS wins: {dfsWins}");
			Console.WriteLine($"BFS wins: {bfsWins}");
			Console.WriteLine($"Ties: {ties}");

			if (dfsWins > bfsWins)
			{
				Console.WriteLine($"\n🏆 DFS is faster for this circuit!");
			}
			else if (bfsWins > dfsWins)
			{
				Console.WriteLine($"\n🏆 BFS is faster for this circuit!");
			}
			else
			{
				Console.WriteLine($"\n🤝 Both algorithms perform equally!");
			}
		}
	}
}
