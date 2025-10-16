# Godot Architecture Design

This document outlines how the Digital Logic Simulator architecture will map to Godot's systems.

## Architecture Overview

The three-layer architecture remains unchanged:

```
Description Layer (POCO data) ↔ Game Layer (editing) ↔ Simulation Layer (thread)
```

Godot-specific adaptations focus on the Game Layer (UI/interaction) and rendering.

## Threading Architecture

### Current Unity Model

```
Main Thread (Unity)              Simulation Thread
├─ UI/Input                      ├─ Simulator.RunSimulationStep()
├─ Rendering                     ├─ Dirty queue processing
├─ Project.cs                    ├─ Chip processors
└─ ConcurrentQueue ─────────────→└─ Pin propagation
```

### Godot Model (Proposed)

```
Main Thread (Godot)              Simulation Thread
├─ _Process() / _Input()         ├─ Simulator.RunSimulationStep()
├─ _Draw() rendering             ├─ Dirty queue processing
├─ Project.cs                    ├─ Chip processors
└─ ConcurrentQueue ─────────────→└─ Pin propagation
```

**Key Points:**
- Godot's threading model is very similar to Unity
- `System.Threading.Thread` works identically
- `ConcurrentQueue<T>` is standard .NET
- No architectural changes needed

### Thread Synchronization

**Reading simulation state for display:**
```csharp
// In _Process() or _Draw() on main thread
public override void _Process(double delta)
{
    // Safe: simulation thread only writes, main thread only reads pin states
    // for display purposes. States are atomic uint32 values.
    UpdateVisuals();
}
```

**Modifying simulation from main thread:**
```csharp
// Use existing queue system
Simulator.AddPin(chip, pinID, isInput);
Simulator.AddConnection(chip, source, target);
// Applied on sim thread during ApplyModifications()
```

## File Structure

### Proposed Godot Project Structure

```
DLS-Godot/
├─ project.godot
├─ .godot/                        # Godot metadata (gitignored)
├─ Scripts/
│  ├─ Simulation/                 # Direct port from Unity
│  │  ├─ Simulator.cs
│  │  ├─ SimChip.cs
│  │  ├─ SimPin.cs
│  │  ├─ PinState.cs
│  │  └─ ChipProcessors/
│  ├─ Description/                # Direct port from Unity
│  │  ├─ Types/
│  │  │  ├─ ChipDescription.cs
│  │  │  ├─ ProjectDescription.cs
│  │  │  └─ SubTypes/
│  │  └─ Serialization/
│  ├─ SaveSystem/                 # Minor path adaptations
│  │  ├─ Saver.cs
│  │  ├─ Loader.cs
│  │  └─ SavePaths.cs             # Update for Godot paths
│  ├─ Game/                       # Moderate adaptation
│  │  ├─ Project.cs
│  │  ├─ ChipLibrary.cs
│  │  ├─ Elements/
│  │  └─ Interaction/             # Godot input API
│  └─ Graphics/                   # Major rewrite
│     ├─ ChipRenderer.cs          # Custom CanvasItem
│     ├─ WireRenderer.cs
│     └─ UIRenderer.cs
├─ Scenes/
│  ├─ Main.tscn                   # Root scene
│  ├─ UI/
│  │  ├─ MainMenu.tscn
│  │  ├─ ChipLibraryMenu.tscn
│  │  └─ ...
│  └─ World/
│     └─ CircuitEditor.tscn
├─ Resources/                     # Godot resources
│  ├─ Themes/
│  ├─ Fonts/
│  └─ Shaders/
└─ docs/
   ├─ godot-migration-plan.md
   └─ godot-architecture.md
```

## Rendering Architecture

### Unity SebVis Current Approach

```
DrawManager (Unity)
└─ Manages quad batching
   ├─ QuadGenerator: Builds meshes
   ├─ InstancedDrawer: GPU instancing
   └─ Drawer: Immediate-mode API
```

### Godot Rendering Options

#### Option A: Custom CanvasItem (Recommended)

```csharp
public partial class CircuitRenderer : CanvasItem
{
    public override void _Draw()
    {
        // Immediate-mode drawing (similar to SebVis)
        DrawRect(new Rect2(pos, size), color);
        DrawLine(from, to, color, width);
        DrawString(font, pos, text, color);

        // Called automatically when QueueRedraw() is invoked
    }

    public void UpdateCircuit()
    {
        // When simulation state changes
        QueueRedraw();
    }
}
```

**Pros:**
- Most similar to current SebVis architecture
- Simple API, easy to port
- Godot optimizes batching internally
- Good 2D performance

**Cons:**
- CPU-bound for very complex drawings
- Less control over batching

#### Option B: MultiMesh Instancing

```csharp
public partial class CircuitRenderer : Node2D
{
    private MultiMesh chipMultiMesh;
    private MultiMesh wireMultiMesh;

    public void RebuildMeshes()
    {
        // Build arrays of transforms and colors
        // for instanced rendering
    }
}
```

**Pros:**
- Maximum GPU performance
- Scales to thousands of instances

**Cons:**
- More complex implementation
- Harder to port from SebVis
- Less flexible for dynamic content

#### Option C: Hybrid Approach

- CanvasItem for UI, text, and wires (dynamic, frequent updates)
- MultiMesh for chip bodies (static, rare updates)

### Recommendation: Start with Option A

Rationale:
1. Fastest to implement (minimal architecture change)
2. Adequate performance for typical circuits
3. Can profile and optimize to Option B/C later if needed
4. Godot's CanvasItem is highly optimized for 2D

## UI System Mapping

### Unity UI → Godot Control Nodes

| Unity Concept | Godot Equivalent | Notes |
|---------------|------------------|-------|
| Canvas | Control tree | Root UI container |
| RectTransform | Control.rect_* | Position/size/anchors |
| Button | Button | Direct equivalent |
| Label | Label | Direct equivalent |
| Panel | Panel | Direct equivalent |
| ScrollRect | ScrollContainer | Direct equivalent |
| Custom drawing | Control + _draw() | Override _draw() method |

### Menu System Architecture

```
Main Scene (Node2D)
├─ CircuitEditorView (CanvasItem)
│  ├─ ChipRenderer (CanvasItem)
│  ├─ WireRenderer (CanvasItem)
│  └─ Camera2D
└─ UILayer (CanvasLayer)
   ├─ MainMenu (Control)
   ├─ ChipLibraryMenu (Control)
   ├─ ContextMenu (Control)
   └─ BottomBar (Control)
```

**Key Pattern:**
- CanvasLayer keeps UI above game world
- Control nodes for UI
- CanvasItem nodes for custom drawing
- Signals for event communication

## Input System Mapping

### Unity Input → Godot Input

```csharp
// Unity pattern
if (Input.GetMouseButtonDown(0))
    OnLeftClick();

// Godot pattern
public override void _Input(InputEvent @event)
{
    if (@event is InputEventMouseButton mouseEvent)
    {
        if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            OnLeftClick();
    }
}

// Or use InputMap for keyboard shortcuts
if (Input.IsActionJustPressed("place_chip"))
    PlaceChip();
```

### Mouse Position Handling

```csharp
// Unity: Screen space to world space
Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

// Godot: Get mouse position in viewport, transform to canvas space
Vector2 worldPos = GetViewport().GetMousePosition();
worldPos = GetGlobalTransformWithCanvas().AffineInverse() * worldPos;

// Or use get_global_mouse_position() on CanvasItem nodes
Vector2 worldPos = GetGlobalMousePosition();
```

## Serialization & Save System

### Path Differences

```csharp
// Unity paths
string saveDir = Application.persistentDataPath;
// Linux: ~/.config/unity3d/CompanyName/ProductName/

// Godot paths
string saveDir = OS.GetUserDataDir();
// Linux: ~/.local/share/godot/app_userdata/DLS/

// Proposed: Keep existing custom path
string saveDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "DLS"
);
// Linux: ~/.local/share/DLS/ (same as current)
```

**Recommendation:** Keep current path structure for save file compatibility.

### JSON Serialization

```csharp
// Unity uses Newtonsoft.Json (external package)
// Godot also uses Newtonsoft.Json (same package)

// No changes needed to serialization code!
string json = JsonConvert.SerializeObject(projectDesc, settings);
```

### Custom Type Converters

```csharp
// Unity types
Vector2 unityVec;
Color unityColor;

// Godot types
Vector2 godotVec;
Color godotColor;

// Need converters for serialization
public class Vector2Converter : JsonConverter<Vector2>
{
    // Convert between Unity Vector2 and Godot Vector2 during serialization
}
```

## Audio System

### Unity Audio → Godot Audio

```csharp
// Unity pattern
AudioSource.PlayOneShot(clip, volume);

// Godot pattern (C#)
AudioStreamPlayer player = GetNode<AudioStreamPlayer>("BuzzerPlayer");
player.VolumeDb = Mathf.LinearToDb(volume);
player.Play();

// Or for positional audio
AudioStreamPlayer2D player = GetNode<AudioStreamPlayer2D>("BuzzerPlayer");
player.GlobalPosition = chipPosition;
player.Play();
```

**SimAudio.cs adaptation:**
- Keep frequency/note tracking logic
- Replace Unity AudioSource calls with Godot AudioStreamPlayer
- Consider AudioStreamGenerator for synthesized tones

## Camera System

### Unity Camera → Godot Camera2D

```csharp
// Unity pattern
transform.position = newPos;
Camera.main.orthographicSize = zoom;

// Godot pattern
GlobalPosition = newPos;
Zoom = new Vector2(zoomLevel, zoomLevel);

// Godot has built-in features Unity lacks:
// - Drag margins
// - Smooth following
// - Viewport rect limits
```

**CameraController.cs adaptation:**
- Direct mapping to Camera2D properties
- Simpler code (Godot's Camera2D is more 2D-focused)
- Zoom, pan, limits all built-in

## C# and .NET Considerations

### Godot C# Limitations

**What works identically:**
- All standard .NET 8 libraries
- System.Threading
- System.Collections.Concurrent
- Newtonsoft.Json (via NuGet)
- LINQ
- Async/await

**Godot-specific considerations:**
- Must use `partial` keyword for Godot classes
- Properties exposed to editor use `[Export]` attribute
- Node paths use `GetNode<T>()` instead of `FindObjectOfType<T>()`

### Example Class Structure

```csharp
// Unity style
public class ChipEditor : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    void Start() { }
    void Update() { }
}

// Godot style
public partial class ChipEditor : Node2D
{
    [Export] public Camera2D MainCamera { get; set; }

    public override void _Ready() { }
    public override void _Process(double delta) { }
}
```

## Performance Considerations

### Godot Advantages for This Project

1. **Lighter runtime**: Less overhead than Unity
2. **2D-first design**: Better optimized for 2D rendering
3. **Custom rendering**: Direct control via _draw()
4. **Smaller builds**: 10-20MB vs Unity's 50-100MB

### Optimization Strategies

**Phase 1 (MVP):**
- Immediate-mode rendering via _draw()
- Single render pass per frame
- Dirty rectangles for partial updates

**Phase 2 (Optimization):**
- Cull off-screen chips
- Level-of-detail (simplified drawing when zoomed out)
- Cache static geometry

**Phase 3 (Advanced):**
- Compute shaders for large circuits
- Async texture updates for displays
- MultiMesh instancing for chip bodies

## Migration Path Summary

### Low-effort ports (copy + minor tweaks):
- Entire Simulation/ folder
- Entire Description/ folder
- Most of SaveSystem/
- Project.cs, ChipLibrary.cs
- Element instance classes

### Medium-effort adaptations:
- Interaction system (input API differences)
- SavePaths.cs (path changes)
- Audio system (API mapping)

### High-effort rewrites:
- Rendering system (SebVis → Godot)
- UI menus (Unity UI → Control nodes)
- Camera system (different API, though simpler)

## Testing Strategy

### Unit Tests
```csharp
// Godot supports standard .NET testing frameworks
// Use NUnit or xUnit

[Test]
public void TestNandProcessor()
{
    var chip = CreateNandChip();
    chip.InputPins[0].State = 1;
    chip.InputPins[1].State = 1;

    var processor = new NandChipProcessor();
    processor.ProcessChip(chip);

    Assert.AreEqual(0, chip.OutputPins[0].State);
}
```

### Integration Tests
```csharp
// Godot scene-based testing
[Test]
public void TestCircuitSimulation()
{
    var scene = ResourceLoader.Load<PackedScene>("res://Scenes/TestCircuits/AdderTest.tscn");
    var circuit = scene.Instantiate<CircuitEditor>();

    // Run simulation, verify outputs
}
```

## Resources & References

- [Godot C# API Documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)
- [Godot 2D Custom Drawing](https://docs.godotengine.org/en/stable/tutorials/2d/custom_drawing_in_2d.html)
- [Godot Threading](https://docs.godotengine.org/en/stable/tutorials/performance/threads/using_multiple_threads.html)
- [Godot Input Handling](https://docs.godotengine.org/en/stable/tutorials/inputs/input_examples.html)

## Next Steps

1. Set up Godot 4.3+ development environment
2. Create "Hello World" with custom rendering
3. Port minimal simulation core (Phase 1)
4. Validate threading and performance
5. Proceed with full migration if Phase 1 successful
