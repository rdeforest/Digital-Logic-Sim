# Performance Testing Guide

## Overview

This guide explains how to use the hardware-independent metrics system to benchmark and compare the Depth-First (DFS) and Breadth-First (BFS) simulation algorithms.

## Hardware-Independent Metrics

The key innovation is measuring **logical work** rather than wall-clock time, making results reproducible across different computers.

### Primary Metrics

| Metric | Description | Interpretation |
|--------|-------------|----------------|
| **Primitive Evaluations** | Number of primitive chips processed | Total work done |
| **Pin State Changes** | Number of pins that actually changed value | Useful work (causes downstream changes) |
| **Wasted Evaluations** | Chips evaluated but outputs unchanged | Inefficiency indicator |
| **Pin Propagations** | Total signal copies between pins | Communication overhead |
| **Simulation Steps** | Number of simulation frames executed | Test duration |

### Efficiency Ratios

| Ratio | Formula | Ideal Value |
|-------|---------|-------------|
| **Evaluation Efficiency** | 1.0 - (Wasted / Total Evals) | 1.0 (no waste) |
| **Propagation Efficiency** | State Changes / Propagations | 1.0 (every propagation causes change) |
| **Avg Evals/Step** | Primitive Evals / Sim Steps | Lower is better (less work per frame) |

### Algorithm-Specific Metrics

| Metric | DFS | BFS |
|--------|-----|-----|
| **Resort Operations** | N/A | Counts topological re-sorts |
| **Dynamic Reorder Attempts** | Chip swaps for race conditions | N/A |
| **Ready Checks** | "Are inputs ready?" calls | N/A |
| **Max Call Stack Depth** | Maximum recursion depth | N/A (iterative) |

## Using the Metrics System

### Step 1: Enable Metrics Collection

In your test code or main program:

```csharp
using DLS.Simulation;

// Enable metrics (has small performance cost)
Simulator.EnableMetrics = true;

// Reset counters before test
Simulator.Metrics.Reset();
```

### Step 2: Set Circuit Structure Info

Before running simulation, characterize your circuit:

```csharp
// Analyze circuit structure (helper methods needed)
Simulator.Metrics.TotalPrimitives = CountPrimitives(rootChip);
Simulator.Metrics.GraphDepth = CalculateMaxDepth(rootChip);
Simulator.Metrics.GraphWidth = CalculateMaxWidth(rootChip);
Simulator.Metrics.FeedbackLoopCount = CountFeedbackLoops(rootChip);
```

### Step 3: Run Simulation

```csharp
// Switch algorithm
Simulator.CurrentMode = SimulationMode.DepthFirst; // or BreadthFirst

// Run for N steps
for (int i = 0; i < 1000; i++)
{
    Simulator.RunSimulationStep(rootChip, inputPins, audioState);
}
```

### Step 4: Generate Report

```csharp
// Print human-readable report
string report = Simulator.Metrics.GenerateReport("DFS");
Console.WriteLine(report);

// Or export to CSV for analysis
string csv = Simulator.Metrics.ToCSV("DFS");
File.AppendAllText("metrics.csv", csv + "\n");
```

### Step 5: Compare Algorithms

```csharp
// Test DFS
Simulator.CurrentMode = SimulationMode.DepthFirst;
Simulator.Metrics.Reset();
RunTest(circuit, 1000);
string dfsReport = Simulator.Metrics.GenerateReport("DFS");

// Test BFS
Simulator.CurrentMode = SimulationMode.BreadthFirst;
Simulator.Metrics.Reset();
RunTest(circuit, 1000);
string bfsReport = Simulator.Metrics.GenerateReport("BFS");

// Compare
Console.WriteLine(dfsReport);
Console.WriteLine(bfsReport);
```

## Instrumentation Points (Manual)

Due to whitespace sensitivity in automated editing, here are the instrumentation points to add manually:

### In Simulator.cs - RunSimulationStep()

After `simulationFrame++`:

```csharp
// Track simulation step
if (EnableMetrics)
{
    Metrics.RecordSimulationStep();
}
```

### In Simulator.cs - StepChip() (DFS mode)

At the start:

```csharp
static void StepChip(SimChip chip)
{
    if (EnableMetrics) Metrics.EnterChipEvaluation();
    // ... existing code
```

At the end:

```csharp
    // ... existing code
    if (EnableMetrics) Metrics.ExitChipEvaluation();
}
```

Inside the dynamic reorder check:

```csharp
if (canDynamicReorderThisFrame && i > 0 && !nextSubChip.Sim_IsReady() && RandomBool())
{
    if (EnableMetrics) Metrics.RecordReadyCheck();

    SimChip potentialSwapChip = chip.SubChips[i - 1];
    if (!ChipTypeHelper.IsBusOriginType(potentialSwapChip.ChipType))
    {
        if (EnableMetrics) Metrics.RecordDynamicReorderAttempt();
        // ... swap code
    }
}
```

### In Simulator.cs - RunBreadthFirstStep()

After resort:

```csharp
if (rootSimChip != prevRootSimChip || needsResort)
{
    allPrimitiveChips.Clear();
    CollectPrimitiveChips(rootSimChip, allPrimitiveChips);
    sortedPrimitives = TopologicalSort(allPrimitiveChips);
    needsResort = false;

    if (EnableMetrics) Metrics.RecordResort();
}
```

### In Simulator.cs - ProcessBuiltinChip()

Track primitive evaluations:

```csharp
static void ProcessBuiltinChip(SimChip chip)
{
    uint oldOutputState = chip.OutputPins.Length > 0 ? chip.OutputPins[0].State : 0;

    // ... existing chip processing code ...

    if (EnableMetrics)
    {
        bool outputChanged = chip.OutputPins.Length > 0 && chip.OutputPins[0].State != oldOutputState;
        Metrics.RecordPrimitiveEvaluation(outputChanged);
    }
}
```

### In SimPin.cs - PropagateSignal()

Track propagations:

```csharp
public void PropagateSignal()
{
    int length = ConnectedTargetPins.Length;
    for (int i = 0; i < length; i++)
    {
        ConnectedTargetPins[i].ReceiveInput(this);
        if (Simulator.EnableMetrics) Simulator.Metrics.RecordPinPropagation();
    }
}
```

### In SimPin.cs - ReceiveInput() or State setter

Track state changes:

```csharp
// When pin state actually changes
if (newState != State)
{
    State = newState;
    if (Simulator.EnableMetrics) Simulator.Metrics.RecordPinStateChange();
}
```

## Test Circuits

The `TestCircuitGenerator` class provides several pre-built test circuits:

### NOT Chain
```csharp
var circuit = TestCircuitGenerator.CreateNotChain("Chain100", 100);
```
- **Use case:** Tests basic linear propagation
- **Properties:** Depth=100, Width=1, No feedback
- **Expected:** Both algorithms should be similar

### AND Tree
```csharp
var circuit = TestCircuitGenerator.CreateAndTree("Tree8", 8);
```
- **Use case:** Tests wide parallelism and reconvergent paths
- **Properties:** Depth=log2(N), Width=N/2, No feedback
- **Expected:** BFS may be slower (no short-circuit optimization)

### SR Latch
```csharp
var circuit = TestCircuitGenerator.CreateSRLatch("SRLatch");
```
- **Use case:** Tests feedback loop handling
- **Properties:** Small circuit with 2 feedback loops
- **Expected:** Different non-deterministic behavior between algorithms

### Wide Fanout
```csharp
var circuit = TestCircuitGenerator.CreateWideFanout("Fanout50", 50);
```
- **Use case:** Tests single-input driving many gates
- **Properties:** Depth=2, Width=fanout
- **Expected:** Tests propagation overhead

## Interpreting Results

### Comparison Checklist

For each test circuit, compare:

1. **Total Primitive Evaluations**
   - Lower = more efficient
   - Should be identical for combinational circuits
   - May differ for sequential circuits (feedback)

2. **Wasted Evaluations**
   - DFS may have more waste (re-evaluates not-ready chips)
   - BFS should have less waste (processes in order)

3. **Evaluation Efficiency**
   - Higher = better
   - BFS should score higher for combinational logic

4. **Algorithm-Specific Overhead**
   - DFS: Reorder attempts, ready checks, stack depth
   - BFS: Resort operations
   - Compare relative costs

5. **Avg Evaluations per Step**
   - Shows steady-state behavior
   - Lower = less work per simulation frame

### Example Interpretation

```
=== DFS Simulation Metrics ===
Primitive Evals:      150,000
Wasted Evaluations:    15,000
Evaluation Efficiency:   90.0%

=== BFS Simulation Metrics ===
Primitive Evals:      135,000
Wasted Evaluations:     1,000
Evaluation Efficiency:   99.3%
```

**Interpretation:** BFS does 10% less work and wastes almost nothing, suggesting better efficiency for this circuit.

## CSV Export for Analysis

Generate CSV data for spreadsheet analysis:

```csharp
// Write header once
File.WriteAllText("results.csv", SimulationMetrics.GetCSVHeader() + "\n");

// Run multiple tests
foreach (var circuit in testCircuits)
{
    foreach (var mode in new[] { SimulationMode.DepthFirst, SimulationMode.BreadthFirst })
    {
        Simulator.CurrentMode = mode;
        Simulator.Metrics.Reset();
        RunTest(circuit, 1000);

        string modeName = mode == SimulationMode.DepthFirst ? "DFS" : "BFS";
        File.AppendAllText("results.csv", Simulator.Metrics.ToCSV(modeName) + "\n");
    }
}
```

Then open `results.csv` in Excel/Google Sheets to:
- Create comparison charts
- Calculate percentage differences
- Identify which circuits benefit most from each algorithm

## Next Steps

1. **Complete Manual Instrumentation** - Add the instrumentation points listed above to Simulator.cs and SimPin.cs

2. **Test Framework** - Create unit tests that load circuits and run both algorithms

3. **Automated Comparison** - Build script that runs all test circuits through both algorithms

4. **Real-World Circuits** - Test on actual user-created circuits (64k RAM, CPU designs, etc.)

5. **Visualization** - Generate performance comparison graphs

## Questions to Answer

Use this metrics system to answer:

1. **Which algorithm is more efficient for combinational logic?**
   - Measure wasted evaluations on AND tree, NOT chain

2. **Which handles sequential logic better?**
   - Test SR latches, registers, counters

3. **What's the overhead of each algorithm?**
   - Compare resort ops (BFS) vs ready checks (DFS)

4. **Do they produce equivalent results?**
   - Run same inputs, compare outputs

5. **Which scales better?**
   - Test on increasingly large circuits (10, 100, 1000, 10000 gates)

## Troubleshooting

**Metrics are all zero:**
- Check that `Simulator.EnableMetrics = true`
- Verify instrumentation points are added correctly
- Ensure simulation actually runs

**Results seem inconsistent:**
- Call `Metrics.Reset()` before each test
- Run multiple iterations and average
- Check for random behavior (DFS dynamic reordering)

**CSV export fails:**
- Ensure write permissions
- Check file path exists
- Verify CSV header matches data columns
