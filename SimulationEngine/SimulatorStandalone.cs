using System;
using DLS.Description;

namespace DLS.Simulation
{
	/// <summary>
	/// Standalone wrapper for Simulator with simplified API for perf testing.
	/// Removes Unity dependencies.
	/// </summary>
	public static class SimulatorStandalone
	{
		/// <summary>
		/// Build a SimChip from a ChipDescription for standalone simulation.
		/// </summary>
		public static SimChip BuildSimChip(ChipDescription chipDesc)
		{
			var library = new ChipLibrary();
			// Add the chip itself if it's custom
			if (chipDesc.ChipType == ChipType.Custom)
			{
				library.AddChip(chipDesc);
			}
			return Simulator.BuildSimChip(chipDesc, library);
		}

		/// <summary>
		/// Run a single simulation step without inputs or audio.
		/// </summary>
		public static void RunSimulationStep(SimChip rootChip)
		{
			// Simplified version - no inputs, no audio
			if (rootChip != Simulator.prevRootSimChip)
			{
				Simulator.needsOrderPass = true;
				Simulator.prevRootSimChip = rootChip;
			}

			Simulator.pcg_rngState = (uint)Simulator.rng.Next();
			Simulator.canDynamicReorderThisFrame = Simulator.simulationFrame % 100 == 0;
			Simulator.simulationFrame++;

			if (Simulator.EnableMetrics)
			{
				Simulator.Metrics.RecordSimulationStep();
			}

			// No input propagation for standalone testing
			// Just run the simulation

			if (Simulator.CurrentMode == SimulationMode.DepthFirst)
			{
				if (Simulator.needsOrderPass)
				{
					Simulator.StepChipReorder(rootChip);
					Simulator.needsOrderPass = false;
				}
				else
				{
					Simulator.StepChip(rootChip);
				}
			}
			else // BreadthFirst
			{
				Simulator.RunBreadthFirstStep(rootChip);
			}
		}
	}
}
