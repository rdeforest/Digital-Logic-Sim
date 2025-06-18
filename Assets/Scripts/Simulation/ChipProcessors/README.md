# Chip Processors

This directory contains all the chip processors for the Digital Logic Simulator. Each processor handles the specific logic for a particular chip type.

## Architecture

- **BaseChipProcessor**: Abstract base class that handles common workflow (input propagation, processing, output propagation)
- **Individual Processors**: Each chip type has its own processor that implements `ProcessChip()`
- **ChipProcessorFactory**: Factory that manages and provides access to processors by chip type

## Available Processors

### Basic Logic
- **NandChipProcessor**: NAND gate logic
- **TriStateBufferChipProcessor**: Tri-state buffer functionality

### Input/Output
- **ClockChipProcessor**: Clock signal generation
- **KeyChipProcessor**: Keyboard input handling
- **BuzzerChipProcessor**: Audio output
- **PulseChipProcessor**: Pulse generation

### Data Manipulation
- **Split4To1BitChipProcessor**: Splits 4-bit input into 1-bit outputs
- **Split8To4BitChipProcessor**: Splits 8-bit input into two 4-bit outputs  
- **Split8To1BitChipProcessor**: Splits 8-bit input into eight 1-bit outputs
- **Merge1To4BitChipProcessor**: Merges four 1-bit inputs into 4-bit output
- **Merge1To8BitChipProcessor**: Merges eight 1-bit inputs into 8-bit output
- **Merge4To8BitChipProcessor**: Merges two 4-bit inputs into 8-bit output

### Memory & Storage
- **DevRam8BitChipProcessor**: 8-bit RAM functionality
- **Rom256x16ChipProcessor**: 256x16 ROM functionality

### Display
- **DisplayRGBChipProcessor**: RGB display with back-buffer and clock edge detection
- **DisplayDotChipProcessor**: Dot matrix display with back-buffer and clock edge detection

### Communication
- **BusChipProcessor**: Handles all bus types (1-bit, 4-bit, 8-bit and their terminus variants)

### Custom Chips
- **CustomChipProcessor**: Handles custom user-created chips by processing their subchips

## Usage

Processors are automatically selected and used by the factory:

```csharp
// Get processor for a chip type
BaseChipProcessor processor = ChipProcessorFactory.GetProcessor(chipType);

// Process a chip
processor.StepChip(chip);
```

## Adding New Processors

1. Create a new class inheriting from `BaseChipProcessor`
2. Implement the `ChipType` property
3. Implement the `ProcessChip(SimChip chip)` method
4. Register the processor in `ChipProcessorFactory.Initialize()`

## Breadth-First Processing

The system uses a dirty queue for breadth-first signal propagation:
- Pin state changes automatically add chips to the dirty queue
- Chips are processed one at a time from the queue
- Signal changes propagate as a "wave front" through the circuit
