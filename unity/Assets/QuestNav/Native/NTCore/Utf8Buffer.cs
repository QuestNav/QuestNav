using System;
using System.Buffers;
using System.Text;

namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// A string encoded as NUL-terminated UTF-8 for passing to native code without per-call
    /// allocations. Encodes into the caller's scratch span (normally stackalloc'd) and rents from
    /// the shared array pool only when the string doesn't fit. Pin <see cref="Bytes"/> with
    /// <c>fixed</c> for the duration of the native call, then dispose.
    /// </summary>
    public ref struct Utf8Buffer
    {
        /// <summary>
        /// Scratch size for callers to stackalloc; longer strings use a pooled array.
        /// </summary>
        public const int StackSize = 256;

        private readonly byte[] rented;

        /// <summary>
        /// The encoded bytes followed by a NUL terminator, so never empty.
        /// </summary>
        public Span<byte> Bytes { get; }

        /// <summary>
        /// The encoded length in bytes, excluding the NUL terminator.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Encodes <paramref name="value"/>, treating null as an empty string.
        /// </summary>
        /// <param name="value">The string to encode</param>
        /// <param name="scratch">Caller-owned buffer to encode into when large enough</param>
        public Utf8Buffer(string value, Span<byte> scratch)
        {
            value ??= string.Empty;
            int size = Encoding.UTF8.GetByteCount(value) + 1;
            rented = null;
            if (size > scratch.Length)
            {
                rented = ArrayPool<byte>.Shared.Rent(size);
                scratch = rented;
            }

            Length = Encoding.UTF8.GetBytes(value.AsSpan(), scratch);
            scratch[Length] = 0;
            Bytes = scratch.Slice(0, Length + 1);
        }

        /// <summary>
        /// Returns the pooled array, if one was rented.
        /// </summary>
        public void Dispose()
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
