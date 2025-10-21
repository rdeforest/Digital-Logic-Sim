using UnityEngine;

namespace DLS.Description
{
	public enum SimulationMode
	{
		DepthFirst,    // Sebastian's original algorithm
		BreadthFirst   // Proposed breadth-first topological sort
	}

	public struct AppSettings
	{
		public int ResolutionX;
		public int ResolutionY;
		public FullScreenMode fullscreenMode;
		public bool VSyncEnabled;
		public SimulationMode SimMode;

		public static AppSettings Default() =>
			new()
			{
				ResolutionX = 1920,
				ResolutionY = 1080,
				fullscreenMode = FullScreenMode.FullScreenWindow,
				VSyncEnabled = true,
				SimMode = SimulationMode.DepthFirst  // Default to original algorithm
			};
	}
}