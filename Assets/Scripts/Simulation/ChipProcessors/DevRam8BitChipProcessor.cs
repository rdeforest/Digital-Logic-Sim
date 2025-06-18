using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    public class DevRam8BitChipProcessor : BaseChipProcessor
    {
        public override ChipType ChipType => ChipType.dev_Ram_8Bit;

        protected override void ProcessChip(SimChip chip)
        {
            uint addressPin = chip.InputPins[0].State;
            uint dataPin = chip.InputPins[1].State;
            uint writeEnablePin = chip.InputPins[2].State;
            uint resetPin = chip.InputPins[3].State;
            uint clockPin = chip.InputPins[4].State;

            // Detect clock rising edge
            bool clockHigh = PinState.FirstBitHigh(clockPin);
            bool isRisingEdge = clockHigh && chip.InternalState[^1] == 0;
            chip.InternalState[^1] = clockHigh ? 1u : 0;

            // Write/Reset on rising edge
            if (isRisingEdge)
            {
                if (PinState.FirstBitHigh(resetPin))
                {
                    for (int i = 0; i < 256; i++)
                    {
                        chip.InternalState[i] = 0;
                    }
                }
                else if (PinState.FirstBitHigh(writeEnablePin))
                {
                    chip.InternalState[PinState.GetBitStates(addressPin)] = PinState.GetBitStates(dataPin);
                }
            }

            // Output data at current address
            chip.OutputPins[0].State = (ushort)chip.InternalState[PinState.GetBitStates(addressPin)];
        }
    }
}
