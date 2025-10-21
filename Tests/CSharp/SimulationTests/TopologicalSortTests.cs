using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DLS.SimulationTests
{
	/// <summary>
	/// Tests for the topological sort algorithm used in signal propagation.
	/// This tests the core BFS algorithm logic in isolation from the full simulator.
	/// </summary>
	public class TopologicalSortTests
	{
		// Minimal test node to represent chips in the graph
		private class TestNode
		{
			public string Name { get; set; }
			public List<TestNode> Dependencies { get; set; } = new();
			public List<TestNode> Dependents { get; set; } = new();

			public TestNode(string name)
			{
				Name = name;
			}

			public bool HasNoInputs() => Dependencies.Count == 0;
		}

		// Implementation of the BFS topological sort from Simulator.cs
		private List<TestNode> TopologicalSort(List<TestNode> nodes)
		{
			var sorted = new List<TestNode>();
			int index = 0;

			// Start with nodes that have no dependencies (no connected inputs)
			foreach (TestNode node in nodes)
			{
				if (node.HasNoInputs())
				{
					sorted.Add(node);
				}
			}

			// Traverse: for each node, add all nodes it outputs to (if not already in list)
			while (index < sorted.Count)
			{
				TestNode node = sorted[index++];

				// Find all nodes that this node outputs to
				foreach (TestNode dependent in node.Dependents)
				{
					// Add to traversal list if not already present
					// This is the key check that prevents duplicates
					if (!sorted.Contains(dependent))
					{
						sorted.Add(dependent);
					}
				}
			}

			// Ensure we didn't miss any nodes (defensive)
			foreach (TestNode node in nodes)
			{
				if (!sorted.Contains(node))
				{
					sorted.Add(node);
				}
			}

			return sorted;
		}

		[Fact]
		public void TopologicalSort_SimpleChain_ReturnsCorrectOrder()
		{
			// Arrange: A → B → C
			var a = new TestNode("A");
			var b = new TestNode("B");
			var c = new TestNode("C");

			a.Dependents.Add(b);
			b.Dependencies.Add(a);
			b.Dependents.Add(c);
			c.Dependencies.Add(b);

			var nodes = new List<TestNode> { a, b, c };

			// Act
			var sorted = TopologicalSort(nodes);

			// Assert
			Assert.Equal(3, sorted.Count);
			Assert.Equal("A", sorted[0].Name);
			Assert.Equal("B", sorted[1].Name);
			Assert.Equal("C", sorted[2].Name);
		}

		[Fact]
		public void TopologicalSort_NoDuplicates()
		{
			// Arrange: Create a graph with multiple paths
			//     A
			//    / \
			//   B   C
			//    \ /
			//     D
			var a = new TestNode("A");
			var b = new TestNode("B");
			var c = new TestNode("C");
			var d = new TestNode("D");

			a.Dependents.AddRange(new[] { b, c });
			b.Dependencies.Add(a);
			c.Dependencies.Add(a);
			b.Dependents.Add(d);
			c.Dependents.Add(d);
			d.Dependencies.AddRange(new[] { b, c });

			var nodes = new List<TestNode> { a, b, c, d };

			// Act
			var sorted = TopologicalSort(nodes);

			// Assert - should have exactly 4 nodes, no duplicates
			Assert.Equal(4, sorted.Count);
			Assert.Equal(4, sorted.Distinct().Count());

			// Verify order: A must come before B and C, B and C must come before D
			int indexA = sorted.IndexOf(a);
			int indexB = sorted.IndexOf(b);
			int indexC = sorted.IndexOf(c);
			int indexD = sorted.IndexOf(d);

			Assert.True(indexA < indexB, "A should come before B");
			Assert.True(indexA < indexC, "A should come before C");
			Assert.True(indexB < indexD, "B should come before D");
			Assert.True(indexC < indexD, "C should come before D");
		}

		[Fact]
		public void TopologicalSort_CycleHandling()
		{
			// Arrange: Create a feedback loop (SR latch pattern)
			// A ←→ B
			var a = new TestNode("A");
			var b = new TestNode("B");

			a.Dependents.Add(b);
			a.Dependencies.Add(b);
			b.Dependents.Add(a);
			b.Dependencies.Add(b);

			var nodes = new List<TestNode> { a, b };

			// Act
			var sorted = TopologicalSort(nodes);

			// Assert - should still include both nodes without duplicates
			Assert.Equal(2, sorted.Count);
			Assert.Equal(2, sorted.Distinct().Count());
		}

		[Fact]
		public void TopologicalSort_MultipleRoots()
		{
			// Arrange: Two independent chains
			// A → B
			// C → D
			var a = new TestNode("A");
			var b = new TestNode("B");
			var c = new TestNode("C");
			var d = new TestNode("D");

			a.Dependents.Add(b);
			b.Dependencies.Add(a);
			c.Dependents.Add(d);
			d.Dependencies.Add(c);

			var nodes = new List<TestNode> { a, b, c, d };

			// Act
			var sorted = TopologicalSort(nodes);

			// Assert
			Assert.Equal(4, sorted.Count);
			Assert.Equal(4, sorted.Distinct().Count());

			// A must come before B
			Assert.True(sorted.IndexOf(a) < sorted.IndexOf(b));
			// C must come before D
			Assert.True(sorted.IndexOf(c) < sorted.IndexOf(d));
		}

		[Fact]
		public void TopologicalSort_ComplexGraph_NoDuplicates()
		{
			// Arrange: Create a complex graph that could trigger duplicates with naive implementation
			//       A
			//      /|\
			//     B C D
			//      \|/
			//       E
			var a = new TestNode("A");
			var b = new TestNode("B");
			var c = new TestNode("C");
			var d = new TestNode("D");
			var e = new TestNode("E");

			a.Dependents.AddRange(new[] { b, c, d });
			b.Dependencies.Add(a);
			c.Dependencies.Add(a);
			d.Dependencies.Add(a);

			b.Dependents.Add(e);
			c.Dependents.Add(e);
			d.Dependents.Add(e);
			e.Dependencies.AddRange(new[] { b, c, d });

			var nodes = new List<TestNode> { a, b, c, d, e };

			// Act
			var sorted = TopologicalSort(nodes);

			// Assert - this is the critical test for the duplicate bug we fixed
			Assert.Equal(5, sorted.Count);
			Assert.Equal(5, sorted.Distinct().Count());

			// Verify A comes before all others
			int indexA = sorted.IndexOf(a);
			Assert.True(indexA < sorted.IndexOf(b));
			Assert.True(indexA < sorted.IndexOf(c));
			Assert.True(indexA < sorted.IndexOf(d));
			Assert.True(indexA < sorted.IndexOf(e));

			// Verify E comes after B, C, D
			int indexE = sorted.IndexOf(e);
			Assert.True(sorted.IndexOf(b) < indexE);
			Assert.True(sorted.IndexOf(c) < indexE);
			Assert.True(sorted.IndexOf(d) < indexE);
		}

		[Fact]
		public void TopologicalSort_EmptyList()
		{
			// Arrange
			var nodes = new List<TestNode>();

			// Act
			var sorted = TopologicalSort(nodes);

			// Assert
			Assert.Empty(sorted);
		}

		[Fact]
		public void TopologicalSort_SingleNode()
		{
			// Arrange
			var a = new TestNode("A");
			var nodes = new List<TestNode> { a };

			// Act
			var sorted = TopologicalSort(nodes);

			// Assert
			Assert.Single(sorted);
			Assert.Equal("A", sorted[0].Name);
		}
	}
}
