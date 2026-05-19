namespace LibJpegTurboUnity
{
    public class DecompressedImage
    {
        public DecompressedImage(int width, int height, int stride, byte[] data, LJTPixelFormat pixelFormat)
        {
            this.Width = width;
            this.Height = height;
            this.Stride = stride;
            this.Data = data;
            this.PixelFormat = pixelFormat;
        }

        public LJTPixelFormat PixelFormat { get; }

        public int Width { get; }

        public int Height { get; }

        public int Stride { get; }

        public byte[] Data { get; }
    }
}