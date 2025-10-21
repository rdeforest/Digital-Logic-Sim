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

### TopologicalSortTests.cs
Tests the breadth-first topological sort algorithm used for signal propagation.

**Key tests:**
- `TopologicalSort_NoDuplicates` - Verifies the fix for the duplicate chips bug
- `TopologicalSort_ComplexGraph_NoDuplicates` - Stress test with multiple paths
- `TopologicalSort_CycleHandling` - Validates feedback loop handling (SR latches)

This algorithm is critical for correct signal propagation through the circuit graph.

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
