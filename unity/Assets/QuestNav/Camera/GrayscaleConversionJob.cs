using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace QuestNav.Camera
{
    [BurstCompile]
    public struct GrayscaleConversionJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<byte> Source;

        [WriteOnly]
        public NativeArray<byte> Destination;
        public int Width;
        public int Height;
        public int Stride;
        public bool FlipVertically;

        public void Execute(int y)
        {
            int srcRow = FlipVertically ? (Height - 1 - y) * Width * 4 : y * Width * 4;
            int dstRow = y * Stride;

            for (int x = 0; x < Width; x++)
            {
                int srcIdx = srcRow + x * 4;
                // RGBA32: R, G, B, A
                byte r = Source[srcIdx];
                byte g = Source[srcIdx + 1];
                byte b = Source[srcIdx + 2];

                Destination[dstRow + x] = (byte)((r * 77 + g * 150 + b * 29) >> 8);
            }
        }
    }
}
