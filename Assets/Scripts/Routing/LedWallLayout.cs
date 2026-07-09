namespace LedShow.Routing
{
    // Encodes the physical wiring of the GroupeLaps 128x128 LED wall: 64
    // vertical strips, each snaking up one column and down the next. A strip
    // has 259 physical LED positions (3 of them are invisible mounting points,
    // not real pixels) and needs 2 ArtNet universes since 512/3 = 170 < 259.
    //
    // Layout per strip (positions are 1-indexed along the physical wire):
    //   1        - invisible, fixes the strip's base to the frame
    //   2..129   - 128 visible LEDs going up (row 0 = bottom .. row 127 = top)
    //   130      - invisible, fixes the strip's top turn
    //   131..258 - 128 visible LEDs coming back down (row 127 = top .. row 0 = bottom)
    //   259      - invisible, fixes the strip's end to the frame
    //
    // 64 strips x 2 columns each = 128 columns. 4 controllers x 16 strips each
    // = 64 strips; each controller therefore owns 32 local universes (0-31).
    public static class LedWallLayout
    {
        public const int Width = 128;
        public const int Height = 128;

        public const int StripCount = 64;
        public const int StripLength = 259;
        public const int FirstUniverseLedCount = 170; // 512 / 3, rounded down
        public const int UniversesPerStrip = 2;
        public const int StripsPerController = 16;
        public const int UniversesPerController = StripsPerController * UniversesPerStrip; // 32

        public static readonly string[] ControllerIps =
        {
            "192.168.1.45",
            "192.168.1.46",
            "192.168.1.47",
            "192.168.1.48",
        };

        public readonly struct LedAddress
        {
            public readonly int ControllerIndex;
            public readonly int LocalUniverse;  // 0-31 on that controller
            public readonly int ChannelOffset;  // byte offset of the R channel within the universe's 512 DMX bytes

            public LedAddress(int controllerIndex, int localUniverse, int channelOffset)
            {
                ControllerIndex = controllerIndex;
                LocalUniverse = localUniverse;
                ChannelOffset = channelOffset;
            }
        }

        // column/row are 0-indexed (0..127). The 3 invisible mounting positions
        // per strip never get returned here since every (column, row) pair maps
        // to exactly one of the 256 visible LEDs on its strip.
        public static LedAddress GetAddress(int column, int row)
        {
            int stripIndex = column / 2;
            bool goingUp = column % 2 == 0;

            int position = goingUp
                ? 2 + row         // 2..129: bottom (row 0) to top (row 127)
                : 258 - row;      // 131..258: top (row 127) to bottom (row 0)

            int universeOffsetInStrip;
            int channelIndex;
            if (position <= FirstUniverseLedCount)
            {
                universeOffsetInStrip = 0;
                channelIndex = position - 1;
            }
            else
            {
                universeOffsetInStrip = 1;
                channelIndex = position - FirstUniverseLedCount - 1;
            }

            int controllerIndex = stripIndex / StripsPerController;
            int localStripIndex = stripIndex % StripsPerController;
            int localUniverse = localStripIndex * UniversesPerStrip + universeOffsetInStrip;

            return new LedAddress(controllerIndex, localUniverse, channelIndex * 3);
        }
    }
}
