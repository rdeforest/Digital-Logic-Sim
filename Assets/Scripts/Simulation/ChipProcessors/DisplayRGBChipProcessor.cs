using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class DisplayRGBChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.DisplayRGB;

        protected override void ProcessChip(SimChip chip)
        {
            const uint addressSpace = 256;
            uint addressPin = chip.InputPins[0].State;
            uint redPin = chip.InputPins[1].State;
            uint greenPin = chip.InputPins[2].State;
            uint bluePin = chip.InputPins[3].State;
            uint resetPin = chip.InputPins[4].State;
            uint writePin = chip.InputPins[5].State;
            uint refreshPin = chip.InputPins[6].State;
            uint clockPin = chip.InputPins[7].State;

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
                    uint data = (uint)(PinState.GetBitStates(redPin) | (PinState.GetBitStates(greenPin) << 4) | (PinState.GetBitStates(bluePin) << 8));
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
            uint colData = chip.InternalState[PinState.GetBitStates(addressPin)];
            chip.OutputPins[0].State = (ushort)((colData >> 0) & 0b1111); // red
            chip.OutputPins[1].State = (ushort)((colData >> 4) & 0b1111); // green
            chip.OutputPins[2].State = (ushort)((colData >> 8) & 0b1111); // blue
        }
    }
}
