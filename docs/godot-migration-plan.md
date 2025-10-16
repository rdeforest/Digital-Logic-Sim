# Godot Migration Plan

**Target Engine:** Godot 4.3+ with C# support (.NET 8)
**Timeline Estimate:** 4-6 weeks for MVP
**Risk Level:** Medium (well-separated architecture reduces risk)

## Project Objectives Alignment

This migration supports all core objectives:
- **Performance**: Godot's lighter runtime, better 2D performance
- **Predictable Behavior**: Direct control over rendering and simulation
- **Best Practices**: Modern C# patterns, cleaner architecture
- **FOSS-Friendly**: MIT licensed, no vendor lock-in

## Pre-Migration Checklist

- [ ] Validate Unity version works correctly with breadth-first simulation
- [ ] Document any Unity-specific behaviors that need preservation
- [ ] Create performance baseline benchmarks (circuit size, FPS, memory)
- [ ] Archive working Unity build for reference
- [ ] Set up Godot 4.3+ with .NET SDK

## Phase 1: Environment & Proof of Concept (Week 1)

### Goals
- Verify Godot C# workflow
- Validate threading model
- Prove simulation core is portable

### Tasks
1. **Install Godot 4.3+ with .NET support**
   - Download from godotengine.org
   - Verify C# project creation works
   - Install .NET 8 SDK if not present

2. **Create "Hello World" project**
   - Basic window with colored rectangle
   - Confirm C# scripts compile and run
   - Test debugger attachment

3. **Port minimal simulation core**
   - Copy: `PinState.cs`, `SimPin.cs`, `SimChip.cs`, `Simulator.cs`
   - Copy: `BaseChipProcessor.cs`, `NandChipProcessor.cs`
   - Create simple test scene with 2 NAND gates
   - **Success Criteria**: Simulation runs on separate thread, processes logic correctly

4. **Threading validation**
   - Verify `System.Threading.Thread` works same as Unity
   - Test `ConcurrentQueue` for thread-safe modifications
   - Confirm simulation thread isolation from render thread

### Rollback Point
If threading doesn't work as expected, investigate:
- Godot's Worker threads API
- GDScript + C# interop for threading
- Main-thread simulation with optimization

## Phase 2: Core Simulation Engine (Week 2)

### Goals
- Port entire simulation layer
- Achieve feature parity with Unity simulation

### Tasks
1. **Port all chip processors** (`Assets/Scripts/Simulation/ChipProcessors/`)
   - All 19 processor files
   - `ChipProcessorFactory.cs`
   - Test each processor individually

2. **Port supporting simulation files**
   - `SimAudio.cs` (may need Godot audio API adjustments)
   - `SimKeyboardHelper.cs` (will need Godot input remapping)

3. **Create test suite**
   - Unit tests for each chip type
   - Integration tests for circuits
   - Performance benchmarks

4. **Validate dirty queue system**
   - Verify breadth-first propagation works
   - Test level-by-level single-stepping
   - Confirm performance is equivalent or better

### Success Criteria
- All chip processors work identically to Unity version
- Simulation thread stable for extended runs
- Performance matches or exceeds Unity baseline

## Phase 3: Data Layer (Week 2-3)

### Goals
- Port description types
- Maintain save file compatibility

### Tasks
1. **Port Description types** (`Assets/Scripts/Description/`)
   - `ChipDescription.cs`
   - `ProjectDescription.cs`
   - `AppSettings.cs`
   - All SubTypes files

2. **Port serialization system**
   - `Serializer.cs` (Newtonsoft.Json should work identically)
   - Custom converters (Vector2, Color may need Godot type mapping)
   - `UnsavedChangeDetector.cs`

3. **Port save/load system** (`Assets/Scripts/SaveSystem/`)
   - Update `SavePaths.cs` for Godot's user data directory
   - `Saver.cs`, `Loader.cs`
   - `UpgradeHelper.cs` for version compatibility

4. **Validate save file compatibility**
   - Load Unity-created projects in Godot version
   - Verify all chip types load correctly
   - Test version upgrade paths

### Success Criteria
- Can load existing project files without conversion
- Saves are compatible between Unity and Godot versions (during transition)
- File structure remains `~/.local/share/DLS/` on Linux

## Phase 4: Game Layer (Week 3-4)

### Goals
- Port editing logic
- Adapt interaction to Godot's input system

### Tasks
1. **Port project management**
   - `Project.cs` (minimal Unity dependencies)
   - `ChipLibrary.cs`
   - `BuiltinChipCreator.cs`, `BuiltinCollectionCreator.cs`

2. **Port element instances** (`Assets/Scripts/Game/Elements/`)
   - `DevChipInstance.cs`
   - `SubChipInstance.cs`
   - `PinInstance.cs`, `WireInstance.cs`, `DisplayInstance.cs`
   - `DevPinInstance.cs`

3. **Port interaction system** (`Assets/Scripts/Game/Interaction/`)
   - `ChipInteractionController.cs` → Godot's input events
   - `KeyboardShortcuts.cs` → Godot's InputMap
   - `UndoController.cs` (no Unity dependencies)
   - `CameraController.cs` → Godot's Camera2D

4. **Adapt interaction patterns**
   - Unity's `Input.GetMouseButton()` → Godot's `Input.is_mouse_button_pressed()`
   - Unity's `Input.GetKey()` → Godot's `Input.is_key_pressed()`
   - Mouse positions and coordinate systems

### Success Criteria
- Can create, edit, and delete chips
- Undo/redo works
- Camera controls feel identical
- All keyboard shortcuts functional

## Phase 5: Rendering System (Week 4-5)

### Goals
- Replace SebVis with Godot rendering
- Maintain visual appearance

### Tasks
1. **Analyze SebVis architecture**
   - Document drawing primitives used (quads, lines, text)
   - Identify performance-critical rendering paths
   - Extract color schemes and visual constants

2. **Design Godot rendering approach**
   - **Option A**: Custom `CanvasItem` with `_draw()` overrides (immediate mode, similar to SebVis)
   - **Option B**: Procedural mesh generation with MultiMesh (instanced rendering)
   - **Option C**: Hybrid (static meshes for chips, immediate for wires)

3. **Implement wire renderer**
   - `WireDrawer.cs` → Godot equivalent
   - Bezier curves or line segments
   - Color coding for signal states

4. **Implement chip renderer**
   - `WorldDrawer.cs` → Godot equivalent
   - Chip outlines, pins, labels
   - Subchip visualization

5. **Implement UI renderer**
   - `UIDrawer.cs` → Godot's Control nodes + custom drawing
   - Menu system using Godot's UI nodes
   - Text rendering with Godot's font system

6. **Port display chips**
   - 7-segment display visualization
   - RGB pixel display
   - Dot matrix display

### Success Criteria
- Visual appearance matches Unity version
- Performance is equivalent or better (target 60 FPS with 1000+ chips)
- Responsive at all zoom levels
- Text rendering crisp and readable

### Rendering Architecture Decision

Recommend **Option A (Custom CanvasItem)** because:
- Most similar to current SebVis immediate-mode approach
- Minimal architectural change
- Godot's `_draw()` is optimized for 2D
- Can optimize later with instancing if needed

## Phase 6: UI & Menus (Week 5-6)

### Goals
- Recreate all menus and UI elements
- Match or improve UX

### Tasks
1. **Port menu system** (`Assets/Scripts/Graphics/UI/Menus/`)
   - `MainMenu.cs`
   - `ChipLibraryMenu.cs`
   - `PinEditMenu.cs`
   - `RomEditMenu.cs`
   - All other menu files (15 total)

2. **Adapt to Godot UI**
   - Scene-based UI layout
   - Control node hierarchy
   - Signals for UI events

3. **Port preferences system**
   - `PreferencesMenu.cs`
   - `AppSettings.cs` integration
   - Godot's ProjectSettings for defaults

4. **Polish & consistency**
   - Color themes
   - Font sizes and readability
   - Animation/transitions (if desired)

### Success Criteria
- All menus functional and intuitive
- Settings persist correctly
- No UI regressions from Unity version

## Testing Strategy

### Per-Phase Testing
- Unit tests for logic ported each phase
- Manual testing of features
- Performance profiling

### Integration Testing
- End-to-end circuit creation and simulation
- Large circuit stress tests (256+ chips)
- Multi-threaded stability tests (hours of runtime)

### Regression Testing
- Load all test projects from Unity version
- Verify identical behavior
- Performance comparison

## Performance Benchmarks

Track these metrics throughout migration:

| Metric | Unity Baseline | Godot Target |
|--------|----------------|--------------|
| Small circuit (10 chips) FPS | ___ | ≥ baseline |
| Medium circuit (100 chips) FPS | ___ | ≥ baseline |
| Large circuit (1000 chips) FPS | ___ | ≥ baseline |
| Memory usage (100 chips) | ___ | ≤ baseline |
| Startup time | ___ | ≤ baseline |
| Load project time | ___ | ≤ baseline |

## Risk Mitigation

### High-Risk Areas

1. **Threading differences**
   - **Mitigation**: Early POC in Phase 1
   - **Fallback**: Main-thread simulation with frame budgeting

2. **Rendering performance**
   - **Mitigation**: Multiple rendering approaches tested
   - **Fallback**: Simpler visual style, reduce anti-aliasing

3. **Audio system**
   - **Mitigation**: Abstract audio behind interface
   - **Fallback**: Disable audio temporarily

4. **File path differences**
   - **Mitigation**: Abstraction layer for paths
   - **Fallback**: Converter tool for project files

### Medium-Risk Areas

1. **Input handling differences**: Test early, document mappings
2. **Font rendering**: Use Godot's DynamicFont with same TTF files
3. **Serialization edge cases**: Extensive load/save testing

## Rollback Strategy

Each phase has a rollback point:
- Can return to Unity version at any time
- Godot branch kept separate until full feature parity
- Save file compatibility maintained during transition

## Post-Migration Tasks

After reaching feature parity:

1. **Optimization pass**
   - Profile and optimize hot paths
   - Consider GPU compute for large circuits
   - Multi-threaded wire layout calculations

2. **Godot-specific improvements**
   - Plugin system using Godot's GDExtension
   - Scene-based chip templates
   - Visual shader support for displays

3. **Distribution**
   - Export templates for Linux/Windows/Mac
   - Build automation
   - Update documentation

## Success Criteria Summary

The migration is complete when:
- [ ] All Unity features work in Godot
- [ ] Performance meets or exceeds Unity baseline
- [ ] Can load all existing projects
- [ ] Saves are forward-compatible
- [ ] No critical bugs
- [ ] Documentation updated
- [ ] Builds available for all platforms

## Next Steps

1. Complete Unity breadth-first simulation fixes
2. Create performance baseline
3. Begin Phase 1 (Environment & POC)
4. Weekly progress reviews
5. Adjust timeline based on Phase 1 results
