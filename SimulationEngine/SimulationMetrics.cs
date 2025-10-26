using System;
using System.Collections.Generic;
using System.Text;

namespace DLS.Simulation
{
	/// <summary>
	/// Hardware-independent metrics for comparing simulation algorithms.
	/// Tracks logical operations rather than wall-clock time for reproducible benchmarks.
	/// </summary>
	public class SimulationMetrics
	{
		// ===== Primary Metrics (measure logical work) =====

		/// <summary>Total number of primitive chip evaluations performed</summary>
		public long PrimitiveEvaluations { get; private set; }

		/// <summary>Number of pin state changes (actual useful work)</summary>
		public long PinStateChanges { get; private set; }

		/// <summary>Number of chip evaluations where outputs didn't change (wasted work)</summary>
		public long WastedEvaluations { get; private set; }

		/// <summary>Total number of pin-to-pin signal propagations</summary>
		public long PinPropagations { get; private set; }

		/// <summary>Number of simulation steps executed</summary>
		public long SimulationSteps { get; private set; }

		// ===== Algorithm-Specific Overhead =====

		/// <summary>Number of times topological sort was rebuilt (BFS only)</summary>
		public long ResortOperations { get; private set; }

		/// <summary>Number of chip swap attempts for race conditions (DFS only)</summary>
		public long DynamicReorderAttempts { get; private set; }

		/// <summary>Number of times chips checked if inputs are ready (DFS only)</summary>
		public long ReadyChecks { get; private set; }

		/// <summary>Maximum recursion depth reached (DFS only)</summary>
		public int MaxCallStackDepth { get; private set; }

		/// <summary>Current recursion depth (for tracking during simulation)</summary>
		private int currentCallStackDepth = 0;

		// ===== Circuit Structure Info =====

		/// <summary>Total number of primitive chips in circuit</summary>
		public int TotalPrimitives { get; set; }

		/// <summary>Longest path from input to output (graph depth)</summary>
		public int GraphDepth { get; set; }

		/// <summary>Maximum number of chips at same depth level (graph width)</summary>
		public int GraphWidth { get; set; }

		/// <summary>Number of feedback loops in circuit</summary>
		public int FeedbackLoopCount { get; set; }

		// ===== Efficiency Ratios (computed properties) =====

		/// <summary>Useful evaluations / Total evaluations (0.0 to 1.0)</summary>
		public double EvaluationEfficiency =>
			PrimitiveEvaluations > 0 ? 1.0 - ((double)WastedEvaluations / PrimitiveEvaluations) : 0.0;

		/// <summary>State changes / Propagations (0.0 to 1.0)</summary>
		public double PropagationEfficiency =>
			PinPropagations > 0 ? (double)PinStateChanges / PinPropagations : 0.0;

		/// <summary>Average primitive evaluations per simulation step</summary>
		public double AvgEvaluationsPerStep =>
			SimulationSteps > 0 ? (double)PrimitiveEvaluations / SimulationSteps : 0.0;

		/// <summary>Average pin state changes per simulation step</summary>
		public double AvgStateChangesPerStep =>
			SimulationSteps > 0 ? (double)PinStateChanges / SimulationSteps : 0.0;

		/// <summary>Average propagations per simulation step</summary>
		public double AvgPropagationsPerStep =>
			SimulationSteps > 0 ? (double)PinPropagations / SimulationSteps : 0.0;

		// ===== Recording Methods =====

		public void RecordPrimitiveEvaluation(bool outputChanged)
		{
			PrimitiveEvaluations++;
			if (!outputChanged)
			{
				WastedEvaluations++;
			}
		}

		public void RecordPinStateChange()
		{
			PinStateChanges++;
		}

		public void RecordPinPropagation()
		{
			PinPropagations++;
		}

		public void RecordSimulationStep()
		{
			SimulationSteps++;
		}

		public void RecordResort()
		{
			ResortOperations++;
		}

		public void RecordDynamicReorderAttempt()
		{
			DynamicReorderAttempts++;
		}

		public void RecordReadyCheck()
		{
			ReadyChecks++;
		}

		public void EnterChipEvaluation()
		{
			currentCallStackDepth++;
			if (currentCallStackDepth > MaxCallStackDepth)
			{
				MaxCallStackDepth = currentCallStackDepth;
			}
		}

		public void ExitChipEvaluation()
		{
			currentCallStackDepth--;
		}

		/// <summary>Reset all counters to zero</summary>
		public void Reset()
		{
			PrimitiveEvaluations = 0;
			PinStateChanges = 0;
			WastedEvaluations = 0;
			PinPropagations = 0;
			SimulationSteps = 0;
			ResortOperations = 0;
			DynamicReorderAttempts = 0;
			ReadyChecks = 0;
			MaxCallStackDepth = 0;
			currentCallStackDepth = 0;
		}

		/// <summary>
		/// Generate a summary report of all metrics.
		/// Useful for comparing algorithm performance.
		/// </summary>
		public string GenerateReport(string algorithmName)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"=== {algorithmName} Simulation Metrics ===");
			sb.AppendLine();

			sb.AppendLine("Circuit Structure:");
			sb.AppendLine($"  Total Primitives:     {TotalPrimitives,10}");
			sb.AppendLine($"  Graph Depth:          {GraphDepth,10}");
			sb.AppendLine($"  Graph Width:          {GraphWidth,10}");
			sb.AppendLine($"  Feedback Loops:       {FeedbackLoopCount,10}");
			sb.AppendLine();

			sb.AppendLine("Simulation Work:");
			sb.AppendLine($"  Simulation Steps:     {SimulationSteps,10}");
			sb.AppendLine($"  Primitive Evals:      {PrimitiveEvaluations,10}");
			sb.AppendLine($"  Pin State Changes:    {PinStateChanges,10}");
			sb.AppendLine($"  Wasted Evaluations:   {WastedEvaluations,10}");
			sb.AppendLine($"  Pin Propagations:     {PinPropagations,10}");
			sb.AppendLine();

			sb.AppendLine("Efficiency Ratios:");
			sb.AppendLine($"  Evaluation Efficiency: {EvaluationEfficiency,9:P2}");
			sb.AppendLine($"  Propagation Efficiency: {PropagationEfficiency,8:P2}");
			sb.AppendLine($"  Avg Evals/Step:        {AvgEvaluationsPerStep,9:F2}");
			sb.AppendLine($"  Avg Changes/Step:      {AvgStateChangesPerStep,9:F2}");
			sb.AppendLine($"  Avg Propagations/Step: {AvgPropagationsPerStep,9:F2}");
			sb.AppendLine();

			sb.AppendLine("Algorithm-Specific:");
			sb.AppendLine($"  Resort Operations:     {ResortOperations,10}");
			sb.AppendLine($"  Reorder Attempts:      {DynamicReorderAttempts,10}");
			sb.AppendLine($"  Ready Checks:          {ReadyChecks,10}");
			sb.AppendLine($"  Max Call Stack Depth:  {MaxCallStackDepth,10}");

			return sb.ToString();
		}

		/// <summary>
		/// Generate a CSV header for exporting metrics.
		/// </summary>
		public static string GetCSVHeader()
		{
			return "Algorithm,TotalPrimitives,GraphDepth,GraphWidth,FeedbackLoops," +
			       "SimSteps,PrimitiveEvals,PinStateChanges,WastedEvals,PinPropagations," +
			       "EvalEfficiency,PropagationEfficiency,AvgEvalsPerStep,AvgChangesPerStep,AvgPropagationsPerStep," +
			       "ResortOps,ReorderAttempts,ReadyChecks,MaxCallStackDepth";
		}

		/// <summary>
		/// Generate a CSV row with current metrics.
		/// </summary>
		public string ToCSV(string algorithmName)
		{
			return $"{algorithmName},{TotalPrimitives},{GraphDepth},{GraphWidth},{FeedbackLoopCount}," +
			       $"{SimulationSteps},{PrimitiveEvaluations},{PinStateChanges},{WastedEvaluations},{PinPropagations}," +
			       $"{EvaluationEfficiency:F4},{PropagationEfficiency:F4},{AvgEvaluationsPerStep:F4}," +
			       $"{AvgStateChangesPerStep:F4},{AvgPropagationsPerStep:F4}," +
			       $"{ResortOperations},{DynamicReorderAttempts},{ReadyChecks},{MaxCallStackDepth}";
		}
	}
}
