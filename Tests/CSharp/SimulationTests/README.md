# Digital Logic Simulator - Simulation Engine Tests

This directory contains standalone C# tests for the simulation engine, independent of Unity.

## Philosophy

These tests are designed to be:
- **Portable**: No Unity dependencies, can migrate to Godot or other engines
- **VSCode-centric**: Not tied to Unity's test runner
- **Focused**: Test algorithms in isolation from UI/rendering concerns
- **Fast**: Minimal dependencies, quick to run

## Running Tests

### Run all tests
```bash
cd Tests/CSharp/SimulationTests
dotnet test
```

### Run with detailed output
```bash
dotnet test --verbosity detailed
```

### Run specific test
```bash
dotnet test --filter TopologicalSort_NoDuplicates
```

### Run with code coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./coverage/
```

Coverage report will be generated at `coverage/coverage.info` (lcov format).

## Test Structure

### TopologicalSortTests.cs (7 tests - all passing)
Tests the breadth-first topological sort algorithm used for signal propagation.

**Key tests:**
- `TopologicalSort_NoDuplicates` - Verifies the fix for the duplicate chips bug
- `TopologicalSort_ComplexGraph_NoDuplicates` - Stress test with multiple paths
- `TopologicalSort_CycleHandling` - Validates feedback loop handling (SR latches)

This algorithm is critical for correct signal propagation through the circuit graph.

### AlgorithmEquivalenceTests.cs (9 tests - skipped, awaiting Unity integration)
Documents what needs to be tested to ensure DFS and BFS simulation modes produce equivalent results for combinational logic.

**Planned tests:**
- NOT, AND, OR gate equivalence
- Full adder (complex combinational circuit)
- Wide fanout stress test
- Deeply nested custom chips
- Mode switching state preservation
- Race condition behavior documentation (SR latches)

These tests are currently skipped because they require Unity simulation infrastructure. They serve as:
1. Documentation of what needs testing
2. Template for future integration tests
3. Regression test checklist

### PerformanceBenchmarks.cs (8 tests - skipped, awaiting implementation)
Measures and compares performance of DFS and BFS simulation modes.

**Metrics tracked:**
- Cycles per second (simulation throughput)
- Primitives per second (gate evaluations/second)
- Memory usage patterns
- Resort/reorder overhead

**Planned benchmarks:**
- Simple chain (100 NOT gates)
- Wide fanout (1→100→1)
- Deep nesting (100 levels)
- **64k RAM circuit** (extreme case with thousands of gates)
- Sequential logic (counters, registers)
- Mode switching overhead
- Partial evaluation optimization check

These benchmarks will help identify performance regressions and compare algorithm efficiency.

## Current Approach

Rather than linking the full simulation codebase (which has Unity/Game dependencies), we test the core algorithms in isolation. This provides:
1. **Fast feedback loop** - No Unity compilation needed
2. **Clear test focus** - Each test validates one algorithm
3. **Portability** - Easy to migrate when moving away from Unity

## Future Work

When the simulation engine becomes more independent of Unity, we can:
- Extract core simulation logic into a separate library project
- Link that library into these tests for integration testing
- Add tests for full chip processing pipeline
- Test actual circuit JSON loading and execution

For now, algorithm-focused unit tests provide confidence in the core logic while remaining fully portable.

## Dependencies

- **.NET 9.0** - Test framework
- **xUnit** - Test runner (most popular .NET test framework)
- **coverlet** - Code coverage (lcov and other formats)

All dependencies are .NET Standard and work on Linux, macOS, and Windows.
