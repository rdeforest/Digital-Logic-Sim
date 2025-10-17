# Digital Logic Simulator Architecture

**Version:** 1.0.0
**Project Type:** Unity game engine - logic circuit simulator
**Language:** C#
**Core Frameworks:** Unity, Newtonsoft.Json

## Project Overview

Digital Logic Sim (DLS) is an interactive educational tool for designing and simulating digital logic circuits. The architecture cleanly separates the editing interface (game layer) from the simulation engine, allowing independent development and performance optimization. The system supports both built-in logic primitives and user-created custom chips.

## Project Objectives

The current development priorities for this project are:

1. **Performance Improvements** - Optimize simulation speed and responsiveness, especially for large circuits
2. **Predictable Behaviors** - Reduce surprising or unintuitive behavior in the simulator; make the system more deterministic and understandable
3. **Best Practices** - Incorporate better architectural patterns, code quality, and maintainability improvements
4. **Future Migration** - Eventually migrate from Unity to a more FOSS-friendly engine (considering Godot)

When working on this codebase, prioritize changes that align with these objectives. Performance optimizations and behavior clarifications are highly valued. Keep portability in mind when making architectural decisions.

## Core Architecture Layers

### 1. Simulation Engine (`Assets/Scripts/Simulation/`)

The simulation layer runs on a separate thread from the main game and performs all logic processing independently of UI/rendering.

**Key Components:**

- **Simulator.cs** - Static orchestrator managing the entire simulation
  - Maintains dirty chip queue (chips needing processing this frame)
  - Builds SimChip tree from ChipDescription during initialization
  - Applies thread-safe modifications from main thread queue
  - Tracks simulation frame counter for state synchronization
  - Two-pass algorithm: ordering pass (determines processing sequence), then incremental updates

- **SimChip.cs** - Runtime representation of a chip instance
  - Holds array of InputPins, OutputPins, SubChips, and internal state
  - Immutable after creation (ChipType, ID, SubChips array)
  - Dynamically resizable pin arrays for editing
  - Processes itself via ChipProcessorFactory polymorphism
  - Recursive structure: custom chips contain subchips recursively

- **SimPin.cs** - Individual pin state and signal propagation
  - State is uint32 (bitfield + tri-state flags, 16 bits each)
  - Tracks input connections (handles conflicting signals randomly)
  - PropagateSignal() copies state to all ConnectedTargetPins
  - Marks parent chip dirty when state changes
  - Records last update frame to detect multi-input conflicts

- **ChipProcessors/** - Pluggable execution strategy pattern
  - BaseChipProcessor abstract base with StepChip() template
  - Each ChipType has dedicated processor (NandChipProcessor, ClockChipProcessor, etc.)
  - CustomChipProcessor just handles dynamic reordering; subchip work via dirty queue
  - ChipProcessorFactory singleton manages processor lookup dictionary

- **PinState.cs** - Tri-state logic encoding (0=low, 1=high, 2=disconnected)
  - Packed into uint as: lower 16 bits = signals, upper 16 bits = tri-state flags
  - Handles multi-bit pin conversions (Split/Merge chips)

**Simulation Loop (RunSimulationStep):**

1. Copy player input states to root chip input pins
2. If first frame or chip modified: StepChipReorder() - determine optimal processing order
3. Otherwise: ProcessOneDirtyChip() from queue
4. When chip processes: inputs propagate → processor runs → outputs propagate
5. Pin state changes automatically add parent chip to dirty queue
6. Continue until no chips remain dirty

**Thread Safety:**

- Modification queue (ConcurrentQueue) accepts main thread changes
- ApplyModifications() called only on simulation thread before processing
- Dirty chip queue prevents data races on pin state reads

### 2. Description Layer (`Assets/Scripts/Description/`)

Pure data structures representing chip definitions (no runtime behavior). Used for persistence and building simulation structures.

**Core Types:**

- **ChipDescription** - Defines a chip template
  - Name (case-insensitive with NameComparer)
  - ChipType enum (Nand, Clock, Custom, etc.)
  - Arrays of InputPins[], OutputPins[], SubChips[], Wires[]
  - Color, Size, Displays[] metadata

- **SubChipDescription** - References a subchip instance within parent
  - Name, ID (unique within parent), Label, Position
  - InternalData[] for ROM/RAM/KEY chip-specific data
  - OutputPinColourInfo[] for visual customization

- **WireDescription** - Connection between two pins
  - SourcePinAddress, TargetPinAddress (PinAddress = {PinOwnerID, PinID})
  - Visual routing points and connection type metadata

- **PinDescription** - Individual pin template
  - ID (unique within chip), Name, BitCount enum (1/4/8-bit)

**Related:**

- **ProjectDescription** - Project metadata, starred chips, collections
- **AppSettings** - User preferences (resolution, vsync, sim speed)

**Key Pattern:** All Description types are POCO (plain old C# objects) for JSON serialization. No methods except convenience comparisons.

### 3. Game Layer (`Assets/Scripts/Game/`)

Everything for interactive editing and visualization. Runs on main thread.

**Project & Editing:**

- **Project.cs** - Top-level coordinator
  - Holds ProjectDescription, ChipLibrary
  - Manages chip view stack (nested editing, enter/return)
  - Spawns simulation thread in background
  - Buffers edits for thread-safe handoff to simulator

- **DevChipInstance.cs** - Currently-being-edited chip
  - Lists of Elements (SubChipInstance, PinInstance, WireInstance, DisplayInstance)
  - Builds SimChip via DescriptionCreator → Simulator.BuildSimChip()
  - Tracks LastSavedDescription for undo/unsaved change detection
  - UndoController manages edit history

- **ChipLibrary.cs** - Dictionary of all available chips
  - Separates builtin vs custom chips internally
  - Both accessible via unified GetChipDescription(name) lookup
  - Automatically hides BusTerminus and dev_Ram_8Bit from menus
  - NotifyChipSaved() updates on save; RemoveChip() on delete

- **SubChipInstance.cs** - Reference to a placed chip instance
  - Immutable Description and ChipType
  - Mutable: Position, Label, InternalData
  - Calculates min size from pin counts
  - Tracks all pins via AllPins array (input+output)

- **PinInstance.cs, WireInstance.cs, DisplayInstance.cs** - Individual elements
- **DevPinInstance.cs** - Input/output ports on the currently-edited chip

**Built-in Chips:**

- **BuiltinChipCreator.cs** - Factory generating ChipDescription for all built-ins
  - Nand (1-bit logic gate)
  - Clock, Pulse, Key (input sources)
  - TriStateBuffer, Bus/BusTerminus (fan-out/multi-source)
  - Split/Merge (bit width conversion)
  - Displays (7-seg, RGB, Dot, LED)
  - ROM_256x16, dev_Ram_8Bit (memory)
  - Buzzer (audio output)

- **BuiltinCollectionCreator.cs** - UI menu organization

**Interaction & UI:**

- **ChipInteractionController.cs** - Handle mouse/keyboard for editing
- **KeyboardShortcuts.cs** - Global hotkeys
- **UndoController.cs** - Edit history with undo/redo

**Graphics:**

- **Graphics/** folder - Rendering (uses custom SebVis drawing library, not detailed here)
- **SaveSystem/** - JSON serialization + file I/O

### 4. Save/Load System (`Assets/Scripts/SaveSystem/`)

**Serializer.cs** - JSON marshalling wrapper
- Custom Vector2 and Color converters
- Uses Newtonsoft.Json for round-tripping

**Loader.cs** - Building runtime structures from disk
- LoadProjectDescription() reads JSON → ProjectDescription
- LoadChipLibrary() combines custom chips + builtins
- Handles version compatibility via UpgradeHelper

**Saver.cs** - Persisting to disk
- SaveProjectDescription() timestamps and writes
- SaveChip() individual chip JSON
- CloneChipDescription() deep copy via round-trip serialization

**SavePaths.cs** - Directory layout (~/.local/share/DLS/)
- AllData/
  - Projects/
    - {ProjectName}/
      - description.json
      - Chips/
        - {ChipName}.json
        - DeletedChips/
  - AppSettings.json

## Data Flow & Key Patterns

### Chip Definition vs Instance

- **Description** (ChipDescription, SubChipDescription): Immutable template, serialized to JSON
- **Game Instance** (DevChipInstance, SubChipInstance): Editable working copy on main thread
- **Simulation Instance** (SimChip): Optimized runtime structure for simulation thread

Flow: Description → DevChipInstance (edit) → DescriptionCreator → ChipDescription (save) → Simulator.BuildSimChip() → SimChip

### Built-in vs Custom Chips

**Built-ins:**
- Generated at runtime via BuiltinChipCreator
- Cannot be deleted, always available
- Processors hardcoded in ChipProcessorFactory

**Custom:**
- Loaded from project's Chips/ folder
- Fully user-defined; built from subchips + wires
- CustomChipProcessor delegates to subchip dirty queue
- Can be edited, renamed, deleted (with backup)

**Namespace Collision:** If custom chip shares name with new built-in (after update), custom takes precedence. Old built-in hidden from library.

### Pin State: Tri-State Logic

Pin state encodes three-state logic (low/high/disconnected) to handle multiple sources (buses, switches, open-drain logic).

```
uint pinState:
  [upper 16 bits] tri-state flags (1 = disconnected, 0 = driven)
  [lower 16 bits] bit values (1 = high, 0 = low)
```

Signal reception on multi-input pins:
- First input this frame: accepted
- Subsequent input: random choice between AND/OR of conflicting bits
- Tri-stated bits always accept input (high-impedance)
- Randomness randomized per frame for race condition variety

### Dirty Queue Algorithm

Key insight: Only process chips whose inputs changed. Pin state changes trigger parent chip dirty marking.

**Normal frames:**
1. Start with root chip in queue (or continue from prior frame)
2. Dequeue chip, run processor
3. Output propagation may mark other chips dirty
4. Repeat until queue empty

**Ordering frames** (first frame or after modification):
1. StepChipReorder() does full traversal with random fallback
2. Establishes processing sequence for subsequent frames
3. Allows deterministic ordering for combinational logic

**Race condition handling:**
- Every 100 frames, CustomChipProcessor randomly reorders adjacent ready chips
- Gives different resolution outcomes for simultaneous updates (SR latches)

## File Organization

```
Assets/Scripts/
├── Simulation/              # Runs on separate thread
│   ├── Simulator.cs         # Main orchestrator
│   ├── SimChip.cs           # Runtime chip instance
│   ├── SimPin.cs            # Pin state + propagation
│   ├── ChipProcessors/      # Strategy pattern processors
│   ├── PinState.cs          # Tri-state encoding
│   └── SimAudio.cs          # Audio state
├── Description/             # POCO data for serialization
│   ├── Types/
│   │   ├── ChipDescription.cs
│   │   ├── ProjectDescription.cs
│   │   ├── SubTypes/
│   │   │   ├── ChipTypes.cs # Chip type enum
│   │   │   ├── PinDescription.cs
│   │   │   └── WireDescription.cs
│   └── Serialization/
│       ├── Serializer.cs    # JSON marshalling
│       └── UnsavedChangeDetector.cs
├── Game/                    # Main thread - editing & UI
│   ├── Main/
│   │   └── Main.cs          # Entry point, project loading
│   ├── Project/
│   │   ├── Project.cs       # Top coordinator
│   │   ├── DevChipInstance.cs # Currently-edited chip
│   │   ├── ChipLibrary.cs   # Chip registry
│   │   └── BuiltinChipCreator.cs
│   ├── Elements/
│   │   ├── SubChipInstance.cs # Placed chip
│   │   ├── PinInstance.cs
│   │   ├── WireInstance.cs
│   │   └── DisplayInstance.cs
│   └── Interaction/
│       ├── ChipInteractionController.cs
│       └── UndoController.cs
├── SaveSystem/              # Disk I/O
│   ├── Saver.cs
│   ├── Loader.cs
│   ├── SavePaths.cs
│   └── Serializer.cs
└── Graphics/                # Rendering (SebVis library)
```

## Common Development Tasks

### Adding a New Built-in Chip Type

1. **Add ChipType enum** in `ChipTypes.cs`
2. **Create processor** in `ChipProcessors/` inheriting BaseChipProcessor
   - Override ProcessChip() with logic
   - Override ChipType property
3. **Create descriptor factory** in BuiltinChipCreator
   - Call CreateBuiltinChipDescription() helper
4. **Register processor** in ChipProcessorFactory.Initialize()
5. **Add to output** in BuiltinChipCreator.CreateAllBuiltinChipDescriptions()

### Modifying Chip Simulation Logic

- Edit processor's ProcessChip() method
- Read input pin states: `chip.InputPins[i].State`
- Modify output pin states: `chip.OutputPins[i].State = newValue`
- Pin state setter automatically marks chip dirty when changed
- For internal state (ROM/RAM): access `chip.InternalState[]` directly

### Adding/Removing Pins at Runtime

- **Main thread:** Queue modification via Simulator.AddPin() / RemovePin()
- **Sim thread:** ApplyModifications() calls SimChip.AddPin() / RemovePin()
- Resizes pin arrays; triggers needsOrderPass

### Saving/Loading Custom Chips

- User edits DevChipInstance
- DescriptionCreator.CreateChipDescription() converts to description
- Saver.SaveChip() JSON encodes and writes to Chips/ folder
- Loader.LoadChipLibrary() reads back, registers in ChipLibrary
- Next time opened: Simulator.BuildSimChip() builds from description

## Performance Considerations

**Dirty Queue Optimization:** Only processes chips whose inputs changed this frame. Significant speedup for large circuits.

**Processor Factory:** Dictionary lookup instead of switch statement for better scalability.

**Array Over Collections:** Pin arrays sized exactly (Array.Resize); no LinkedList overhead.

**Thread Isolation:** Simulation thread independent from main thread minimizes locks.

**Caution:** ModificationQueue is ConcurrentQueue but still vulnerable if simulation reads while modifying. Comment notes this risk for future improvement.

## Version Compatibility

Projects store DLSVersion_LastSaved and DLSVersion_EarliestCompatible. UpgradeHelper applies schema migrations when loading old projects. Allows forward/backward compatibility within defined range.

## Testing & Debugging

- **SanityTests.cs** - Newtonsoft serialization unit tests
- **debug_logSimTime** - Toggles sim performance logging
- **debug_runSimMainThread** - Forces simulation on main thread for debugging

## Key Architectural Insights

1. **Separation of Concerns:** Description ↔ Game ↔ Simulation are distinct
2. **Simulation Independence:** Can be tested/optimized separately; threading hidden from game logic
3. **Polymorphism via Factory:** Adding chip types requires no switch statement modifications
4. **Lazy Computation:** Dirty queue avoids reprocessing unchanged state
5. **Immutable Descriptions:** JSON round-trip for cloning; no manual deep copy logic
6. **Tri-State Encoding:** Compact bit-packing allows efficient multi-source handling

## Gotchas & Notes

- **Pin lookup is O(n):** GetSimPinFromAddress() iterates arrays; could be dict for large chips
- **Race conditions with threads:** Comments warn of access from main/sim thread simultaneously; mitigated by modification queue
- **Bus terminus hidden:** Automatically created; user can't place directly
- **Custom chips can shadow built-ins:** Name collision resolved in built-in's favor; might confuse users
- **Wires store indices:** Can fail to load if pins deleted from referenced chips (handled gracefully)
- **ROM/RAM state:** Serialized in SubChipDescription.InternalData; must update if chip modified

## Recommended Reading Order

1. **Simulator.cs** - Understand the orchestration and dirty queue
2. **SimChip.cs** - See recursive structure and processor pattern
3. **SimPin.cs** - Understand pin state and signal propagation
4. **ChipDescription.cs** - See the data model
5. **ChipLibrary.cs** - Built-in vs custom chip handling
6. **BuiltinChipCreator.cs** - Pattern for defining new chips
7. **Project.cs** - How game layer coordinates everything
8. Individual processor implementations as needed

## Current Work: Breadth-First Propagation

### Changes Made (Simulator.cs:114-120)

Modified the simulation loop to process signals in level-by-level waves (like Dijkstra's algorithm edge propagation):

```csharp
// Old: Process only ONE chip per frame
ProcessOneDirtyChip();

// New: Process entire current wave per frame
int chipsInCurrentWave = dirtyChips.Count;
for (int i = 0; i < chipsInCurrentWave; i++)
{
    ProcessOneDirtyChip();
}
```

**Goal:** Allow single-stepping through logic propagation one "hop" at a time for debugging and visualization.

### Known Issue Being Debugged

**Symptom:** Signal propagates to chip input (wire toggles) but chip doesn't process.

**Test case:**
- 2 NANDs wired in feedback loop (alternates correctly ✓)
- First NAND output → OR gate input (wire toggles ✓)
- OR gate output never goes live (✗)

**Hypothesis:** OR isn't being added to dirty queue when its input changes.

**Key mechanism to verify:**
1. `SimPin.State` setter (SimPin.cs:21) should call `Simulator.AddDirtyChip(parentChip)`
2. `AddDirtyChip()` (Simulator.cs:45) uses `Contains()` check - might prevent re-queueing?

**Debugging approach:**
- Use Unity debugger attached to inspect dirtyChips queue state
- Check if OR is in queue but not being processed
- Verify pin state changes are triggering dirty marking

### Future Godot Migration

See `godot-migration` branch for:
- `docs/godot-migration-plan.md` - 6-phase migration timeline
- `docs/godot-architecture.md` - Technical architecture mapping

Migration will use **GDScript** (not C#) to avoid Microsoft/CLR dependency while maintaining FOSS principles.
