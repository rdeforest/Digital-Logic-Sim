using System;
using System.Collections.Generic;
using DLS.Description;

namespace DLS.Simulation.ChipProcessors
{
    /// <summary>
    /// Factory class that manages chip processors and provides access to them by chip type.
    /// This replaces the large switch statement in ProcessBuiltinChip with a more modular approach.
    /// </summary>
    public static class ChipProcessorFactory
    {
        private static readonly Dictionary<ChipType, BaseChipProcessor> processors = new();
        private static bool initialized = false;

        /// <summary>
        /// Initialize the factory with all available chip processors
        /// </summary>
        public static void Initialize()
        {
            if (initialized) return;

            // Register all chip processors
            RegisterProcessor(new CustomChipProcessor());
            RegisterProcessor(new NandChipProcessor());
            RegisterProcessor(new ClockChipProcessor());
            RegisterProcessor(new PulseChipProcessor());
            RegisterProcessor(new TriStateBufferChipProcessor());
            RegisterProcessor(new Split4To1BitChipProcessor());
            RegisterProcessor(new Merge1To4BitChipProcessor());
            RegisterProcessor(new KeyChipProcessor());
            RegisterProcessor(new BuzzerChipProcessor());
            RegisterProcessor(new DevRam8BitChipProcessor());

            // Register the newly created chip processors
            RegisterProcessor(new Merge1To8BitChipProcessor());
            RegisterProcessor(new Merge4To8BitChipProcessor());
            RegisterProcessor(new Split8To4BitChipProcessor());
            RegisterProcessor(new Split8To1BitChipProcessor());
            RegisterProcessor(new DisplayRGBChipProcessor());
            RegisterProcessor(new DisplayDotChipProcessor());
            RegisterProcessor(new Rom256x16ChipProcessor());

            // Register bus processor for all bus types
            var busProcessor = new BusChipProcessor();
            processors[ChipType.Bus_1Bit] = busProcessor;
            processors[ChipType.BusTerminus_1Bit] = busProcessor;
            processors[ChipType.Bus_4Bit] = busProcessor;
            processors[ChipType.BusTerminus_4Bit] = busProcessor;
            processors[ChipType.Bus_8Bit] = busProcessor;
            processors[ChipType.BusTerminus_8Bit] = busProcessor;

            initialized = true;
        }

        /// <summary>
        /// Register a chip processor for a specific chip type
        /// </summary>
        /// <param name="processor">The processor to register</param>
        private static void RegisterProcessor(BaseChipProcessor processor)
        {
            processors[processor.ChipType] = processor;
        }

        /// <summary>
        /// Get the processor for a specific chip type
        /// </summary>
        /// <param name="chipType">The chip type to get a processor for</param>
        /// <returns>The processor for the chip type, or null if not found</returns>
        public static BaseChipProcessor GetProcessor(ChipType chipType)
        {
            if (!initialized)
            {
                Initialize();
            }

            processors.TryGetValue(chipType, out BaseChipProcessor processor);
            return processor;
        }

        /// <summary>
        /// Check if a processor exists for the given chip type
        /// </summary>
        /// <param name="chipType">The chip type to check</param>
        /// <returns>True if a processor exists, false otherwise</returns>
        public static bool HasProcessor(ChipType chipType)
        {
            if (!initialized)
            {
                Initialize();
            }

            return processors.ContainsKey(chipType);
        }

        /// <summary>
        /// Process a chip using its registered processor
        /// </summary>
        /// <param name="chip">The chip to process</param>
        /// <returns>True if the chip was processed, false if no processor was found</returns>
        public static bool ProcessChip(SimChip chip)
        {
            BaseChipProcessor processor = GetProcessor(chip.ChipType);
            if (processor != null)
            {
                processor.StepChip(chip);
                return true;
            }
            return false;
        }
    }
}
