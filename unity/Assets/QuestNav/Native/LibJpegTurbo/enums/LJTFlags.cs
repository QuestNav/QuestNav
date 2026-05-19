using System;

namespace LibJpegTurboUnity
{
    [Flags]
    public enum LJTFlags
    {
        None = 0,
        BottomUp = 2,
        FastUpsample = 256,     // 0x00000100
        NoRealloc = 1024,       // 0x00000400
        FastDct = 2048,         // 0x00000800
        AccurateDct = 4096      // 0x00001000
    }
}
