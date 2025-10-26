# Performance Testing Framework

## Overview

This is a standalone, YAML-based performance testing framework for comparing the Depth-First Search (DFS) and Breadth-First Search (BFS) simulation algorithms.

## Key Features

- **Hardware-Independent Metrics** - Measures logical operations, not wall-clock time
- **CPU Performance Metrics** - Also tracks actual execution time for real-world comparison
- **YAML Circuit Format** - Simple, declarative circuit definitions
- **Standalone Execution** - No Unity dependency, runs as console app

## Quick Start

```bash
# Build the tool
dotnet build Tests/PerfTest/PerfTest.csproj

# Run a single test
bin/perf-test tests/circuits/nand-loop.yaml

# Test DFS only
bin/perf-test tests/circuits/not-chain-10.yaml --mode dfs

# Test BFS only
bin/perf-test tests/circuits/not-chain-10.yaml --mode bfs

# Run all tests and compare
bin/compare-all
```

## YAML Circuit Format

Circuits are defined in a simple YAML format:

```yaml
---
name: My Circuit Name
cycles: 1000  # Number of simulation steps to run

circuit:
  # Optional: circuit inputs
  inputs:
    - in0: 1bit
    - in1: 1bit

  # Optional: circuit outputs
  outputs:
    - out0: 1bit

  # Parts (subchips/gates)
  parts:
    - nand0: {type: nand}
    - nand1: {type: nand}

  # Wiring between parts
  wires:
    # Single target
    - from: in0
      to: nand0.in0

    # Multiple targets (fan-out)
    - from: nand0.out0
      to: [nand1.in0, nand1.in1, out0]
```

### Pin Addressing

- **Chip-level pins**: `in0`, `out0` (refers to circuit inputs/outputs)
- **Part pins**: `nand0.in0`, `nand0.out0` (partName.pinName)
- **Pin numbering**: NAND gates have `in0`, `in1`, `out0`

### Supported Bit Widths

- `1bit` or `1` - Single bit
- `4bit` or `4` - 4-bit bus
- `8bit` or `8` - 8-bit bus

### Supported Part Types

Currently:
- `nand` - 2-input NAND gate

(More can be added to `ChipLibrary.cs`)

## Metrics Explained

### Logical Work Metrics

These measure the **amount of computation**, independent of hardware speed:

| Metric | Description | Lower is Better? |
|--------|-------------|------------------|
| **Primitive Evaluations** | Total gate evaluations performed | Yes |
| **Pin State Changes** | Pins that actually changed value | No (shows useful work) |
| **Wasted Evaluations** | Gates evaluated but output unchanged | Yes |
| **Evaluation Efficiency** | 1.0 - (Wasted / Total) | Higher is better |

### Algorithm Overhead

**DFS-specific:**
- **Reorder Attempts** - Dynamic chip swaps for race conditions
- **Ready Checks** - "Are inputs ready?" checks
- **Max Stack Depth** - Maximum recursion depth

**BFS-specific:**
- **Resort Operations** - Topological sort rebuilds

### CPU Performance

These measure **real execution speed**:

| Metric | Description |
|--------|-------------|
| **Wall Time** | Actual elapsed time (milliseconds) |
| **Cycles/Second** | Simulation throughput |
| **Avg Time/Cycle** | Per-step overhead |
| **Evals/Second** | Gate evaluations per second |

## Interpreting Results

### Your Hypothesis

> "DFS wins on circuits with few idle parts, BFS wins with many idle parts"

### How to Test

1. **Few idle parts** - Tight feedback loops, fully connected circuits
   - Example: `nand-loop.yaml` - Every gate always active
   - Expected: DFS wins (less overhead)

2. **Many idle parts** - Sparse circuits, conditional paths
   - Example: Complex circuits with unused branches
   - Expected: BFS wins (skips idle gates)

### Sample Comparison

```
=== DFS ===
Primitive Evaluations: 10,000
Wasted Evaluations:     1,000
Wall Time:              5.234 ms

=== BFS ===
Primitive Evaluations:  9,000
Wasted Evaluations:       100
Wall Time:              4.891 ms

Conclusion: BFS is 10% more efficient (less work) AND 6.5% faster (less time)
```

## Adding New Test Circuits

Create a new YAML file in `tests/circuits/`:

```bash
cat > tests/circuits/my-test.yaml <<EOF
---
name: My Test Circuit
cycles: 1000
circuit:
  parts:
    - nand0: {type: nand}
  wires:
    - from: nand0.out0
      to: [nand0.in0, nand0.in1]
EOF

bin/perf-test tests/circuits/my-test.yaml
```

## Example Circuits

### nand-loop.yaml
- Single NAND gate with output fed back to both inputs
- Oscillates every cycle
- Tests: Minimal circuit, maximum activity
- Primitives: 1

### not-chain-10.yaml
- Chain of 10 NOT gates (NAND with inputs tied together)
- Linear propagation
- Tests: Sequential processing, no parallelism
- Primitives: 10

### and-gate.yaml
- 2 NANDs wired as AND gate
- Tests: Basic multi-gate circuit
- Primitives: 2

## Output Format

```
=== Loaded Circuit: nand-loop ===
Primitives: 1
Cycles to simulate: 1000

=== Testing DFS ===

Logical Work:
  Primitive Evaluations: 1,000
  Pin State Changes:     1,000
  Wasted Evaluations:    0
  Evaluation Efficiency: 100.00%

Algorithm Overhead:
  Resort Operations:     0
  Reorder Attempts:      0
  Ready Checks:          0
  Max Stack Depth:       1

CPU Performance:
  Wall Time:             0.234 ms
  Cycles/Second:         4,273,504
  Avg Time/Cycle:        0.000234 ms
  Evals/Second:          4,273,504

=== Testing BFS ===
[... similar output ...]
```

## Implementation Details

### Project Structure

```
SimulationEngine/          # Standalone C# library
├── Description/           # Data structures
│   ├── ChipDescription.cs
│   ├── PinDescription.cs
│   └── WireDescription.cs
├── Simulator.cs           # Core simulation (from Unity, copied)
├── SimulatorStandalone.cs # Simplified wrapper
├── SimulationMetrics.cs   # Metrics collector
├── YamlCircuitLoader.cs   # YAML parser
└── ChipLibrary.cs         # Built-in chip definitions

Tests/PerfTest/            # Console application
├── Program.cs             # Main entry point
└── PerfTest.csproj

tests/circuits/            # YAML test cases
├── nand-loop.yaml
├── not-chain-10.yaml
└── and-gate.yaml

bin/                       # Executables
├── perf-test              # Wrapper script
└── compare-all            # Batch comparison
```

### How It Works

1. **Load YAML** - `YamlCircuitLoader` parses circuit definition
2. **Build Simulation** - Creates `SimChip` tree from description
3. **Configure** - Set mode (DFS/BFS), enable metrics
4. **Warm up** - Run 100 cycles (JIT compilation)
5. **Test** - Run N cycles, measure time + metrics
6. **Report** - Output results

### No Unity Dependency

The standalone simulator:
- Removes `SimAudio` (audio output)
- Removes `DevPinInstance` (Unity game types)
- Removes display chips (RGB, 7-segment, etc.)
- Keeps core: NAND gates, wiring, simulation algorithms

## Troubleshooting

**Build errors:**
```bash
cd Tests/PerfTest
dotnet restore
dotnet build
```

**Missing YamlDotNet:**
```bash
cd SimulationEngine
dotnet add package YamlDotNet
```

**Permission denied:**
```bash
chmod +x bin/perf-test
chmod +x bin/compare-all
```

## Future Enhancements

- [ ] Add more built-in chips (AND, OR, XOR, etc.)
- [ ] Support custom chips (hierarchical circuits)
- [ ] Add input stimulus (test vectors)
- [ ] CSV export for spreadsheet analysis
- [ ] Graphing/visualization of results
- [ ] Method call counting instrumentation
- [ ] Memory profiling
- [ ] Multi-threaded testing

## Related Documentation

- `docs/performance-testing-guide.md` - Full metrics documentation
- `docs/load-testing-framework-summary.md` - Implementation overview
- `CLAUDE.md` - Project architecture
