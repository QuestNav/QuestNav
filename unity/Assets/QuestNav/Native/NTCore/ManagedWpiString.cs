using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// Represents a safe handle for unmanaged memory allocated via global heap allocation.
    /// </summary>
    internal sealed class UnmanagedMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedMemoryHandle"/> class.
        /// </summary>
        private UnmanagedMemoryHandle()
            : base(true) { }

        /// <summary>
        /// Allocates unmanaged memory of the specified size.
        /// </summary>
        /// <param name="size">The size, in bytes, to allocate.</param>
        /// <returns>A new <see cref="UnmanagedMemoryHandle"/> wrapping the allocated memory.</returns>
        public static UnmanagedMemoryHandle Alloc(int size)
        {
            var ptr = Marshal.AllocHGlobal(size);
            var handle = new UnmanagedMemoryHandle();
            handle.SetHandle(ptr);
            return handle;
        }

        /// <inheritdoc />
        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }

    /// <summary>
    /// Represents a managed wrapper for a native WpiString struct, handling UTF-8 conversion and unmanaged memory lifetimes.
    /// </summary>
    internal sealed unsafe class ManagedWpiString : IDisposable
    {
        private UnmanagedMemoryHandle _buffer;
        private UnmanagedMemoryHandle _structHandle;
        private bool _disposed;

        /// <summary>
        /// Gets the native pointer to the underlying <see cref="WpiString"/> structure.
        /// </summary>
        public WpiString* NativePointer =>
            (_structHandle == null || _structHandle.IsInvalid)
                ? null
                : (WpiString*)_structHandle.DangerousGetHandle();

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedWpiString"/> class with the specified text.
        /// </summary>
        /// <param name="text">The managed string to convert and pin for unmanaged use.</param>
        public ManagedWpiString(string text)
        {
            if (text == null)
                return;

            var utf8 = Encoding.UTF8.GetBytes(text);

            // Use a try/finally block in the constructor to prevent leaks
            // if a later allocation fails halfway through.
            try
            {
                _buffer = UnmanagedMemoryHandle.Alloc(utf8.Length + 1);
                Marshal.Copy(utf8, 0, _buffer.DangerousGetHandle(), utf8.Length);
                Marshal.WriteByte(_buffer.DangerousGetHandle(), utf8.Length, 0);

                _structHandle = UnmanagedMemoryHandle.Alloc(Marshal.SizeOf<WpiString>());
                var local = new WpiString
                {
                    str = (byte*)_buffer.DangerousGetHandle(),
                    len = new UIntPtr((uint)utf8.Length),
                };

                Marshal.StructureToPtr(local, _structHandle.DangerousGetHandle(), false);
            }
            catch
            {
                // Clean up any partially allocated resources before bubbling the exception
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="ManagedWpiString"/> to a native <see cref="WpiString"/> pointer.
        /// </summary>
        /// <param name="wrapper">The wrapper to convert.</param>
        /// <returns>The native pointer, or <c>null</c> if the wrapper is null.</returns>
        public static implicit operator WpiString*(ManagedWpiString wrapper) =>
            wrapper == null ? null : wrapper.NativePointer;

        /// <summary>
        /// Releases all unmanaged resources associated with this instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _structHandle?.Dispose();
            _buffer?.Dispose();

            _disposed = true;
        }
    }
}
