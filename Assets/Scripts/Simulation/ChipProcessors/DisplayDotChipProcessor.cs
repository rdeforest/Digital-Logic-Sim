using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class DisplayDotChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.DisplayDot;

        protected override void ProcessChip(SimChip chip)
        {
            const uint addressSpace = 256;
            uint addressPin = chip.InputPins[0].State;
            uint pixelInputPin = chip.InputPins[1].State;
            uint resetPin = chip.InputPins[2].State;
            uint writePin = chip.InputPins[3].State;
            uint refreshPin = chip.InputPins[4].State;
            uint clockPin = chip.InputPins[5].State;

            // Detect clock rising edge
            bool clockHigh = PinState.FirstBitHigh(clockPin);
            bool isRisingEdge = clockHigh && chip.InternalState[^1] == 0;
            chip.InternalState[^1] = clockHigh ? 1u : 0;

            if (isRisingEdge)
            {
                // Clear back buffer
                if (PinState.FirstBitHigh(resetPin))
                {
                    for (int i = 0; i < addressSpace; i++)
                    {
                        chip.InternalState[i + addressSpace] = 0;
                    }
                }
                // Write to back-buffer
                else if (PinState.FirstBitHigh(writePin))
                {
                    uint addressIndex = PinState.GetBitStates(addressPin) + addressSpace;
                    uint data = PinState.GetBitStates(pixelInputPin);
                    chip.InternalState[addressIndex] = data;
                }

                // Copy back-buffer to display buffer
                if (PinState.FirstBitHigh(refreshPin))
                {
                    for (int i = 0; i < addressSpace; i++)
                    {
                        chip.InternalState[i] = chip.InternalState[i + addressSpace];
                    }
                }
            }

            // Output current pixel colour
            ushort pixelState = (ushort)chip.InternalState[PinState.GetBitStates(addressPin)];
            chip.OutputPins[0].State = pixelState;
        }
    }
}
