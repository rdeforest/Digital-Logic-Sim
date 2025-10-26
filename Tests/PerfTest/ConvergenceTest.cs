using System;
using System.Collections.Generic;
using System.Diagnostics;
using DLS.Description;
using DLS.Simulation;

namespace DLS.PerfTest
{
	/// <summary>
	/// Runs convergence tests: measures cycles until circuit outputs match expected values.
	/// </summary>
	public class ConvergenceTest
	{
		public class TestResult
		{
			public bool Converged { get; set; }
			public int CyclesToConverge { get; set; }
			public long PrimitiveEvaluations { get; set; }
			public long PinStateChanges { get; set; }
			public double WallTimeMs { get; set; }
			public string FailureReason { get; set; } = "";
		}

		/// <summary>
		/// Run a single test vector and measure cycles to convergence.
		/// </summary>
		public static TestResult RunTestVector(
			ChipDescription chipDesc,
			ChipLibrary library,
			Dictionary<string, int> inputs,
			Dictionary<string, int> expected,
			int maxCycles,
			SimulationMode mode)
		{
			// Build simulation
			var rootChip = Simulator.BuildSimChip(chipDesc, library);

			// Configure
			Simulator.CurrentMode = mode;
			Simulator.EnableMetrics = true;
			Simulator.Metrics.Reset();

			var sw = Stopwatch.StartNew();

			// Apply inputs (set input pin states)
			ApplyInputs(rootChip, chipDesc, inputs);

			// Run until convergence or timeout
			int cycle = 0;
			bool converged = false;

			for (cycle = 0; cycle < maxCycles; cycle++)
			{
				SimulatorStandalone.RunSimulationStep(rootChip);

				// Check if outputs match expected
				if (CheckOutputs(rootChip, chipDesc, expected))
				{
					converged = true;
					break;
				}
			}

			sw.Stop();

			return new TestResult
			{
				Converged = converged,
				CyclesToConverge = converged ? cycle + 1 : maxCycles,
				PrimitiveEvaluations = Simulator.Metrics.PrimitiveEvaluations,
				PinStateChanges = Simulator.Metrics.PinStateChanges,
				WallTimeMs = sw.Elapsed.TotalMilliseconds,
				FailureReason = converged ? "" : "Timeout - did not converge"
			};
		}

		static void ApplyInputs(SimChip rootChip, ChipDescription chipDesc, Dictionary<string, int> inputs)
		{
			foreach (var input in inputs)
			{
				string pinName = input.Key;
				int value = input.Value;

				// Find input pin by name
				var pinDesc = Array.Find(chipDesc.InputPins, p => p.Name == pinName);
				if (pinDesc.Name == null)
				{
					throw new Exception($"Input pin not found: {pinName}");
				}

				// Find corresponding SimPin
				var simPin = Array.Find(rootChip.InputPins, p => p.ID == pinDesc.ID);
				if (simPin == null)
				{
					throw new Exception($"SimPin not found for: {pinName}");
				}

				// Set the pin state
				PinState.Set(ref simPin.State, (uint)value);

				// Propagate to connected pins
				simPin.PropagateSignal();
			}
		}

		static bool CheckOutputs(SimChip rootChip, ChipDescription chipDesc, Dictionary<string, int> expected)
		{
			foreach (var expectedOutput in expected)
			{
				string pinName = expectedOutput.Key;
				int expectedValue = expectedOutput.Value;

				// Find output pin by name
				var pinDesc = Array.Find(chipDesc.OutputPins, p => p.Name == pinName);
				if (pinDesc.Name == null)
				{
					throw new Exception($"Output pin not found: {pinName}");
				}

				// Find corresponding SimPin
				var simPin = Array.Find(rootChip.OutputPins, p => p.ID == pinDesc.ID);
				if (simPin == null)
				{
					throw new Exception($"SimPin not found for output: {pinName}");
				}

				// Get actual value
				int actualValue = PinState.GetUInt(simPin.State);

				if (actualValue != expectedValue)
				{
					return false; // Not converged yet
				}
			}

			return true; // All outputs match!
		}
	}
}
