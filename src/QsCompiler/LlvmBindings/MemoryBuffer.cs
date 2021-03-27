// -----------------------------------------------------------------------
// <copyright file="MemoryBuffer.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm
{
    /// <summary>LLVM MemoryBuffer.</summary>
    public sealed unsafe class MemoryBuffer
    {
        /// <summary>Initializes a new instance of the <see cref="MemoryBuffer"/> class from a file.</summary>
        /// <param name="path">Path of the file to load.</param>
        public MemoryBuffer(string path)
        {
            LLVMMemoryBufferRef handle;
            sbyte* msg;
            if (LLVM.CreateMemoryBufferWithContentsOfFile(path.AsMarshaledString(), (LLVMOpaqueMemoryBuffer**)&handle, &msg) == 0)
            {
                var span = new ReadOnlySpan<byte>(msg, int.MaxValue);
                var errTxt = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
                LLVM.DisposeMessage(msg);
                throw new InternalCodeGeneratorException(errTxt);
            }

            this.BufferHandle = handle;
        }

        /// <summary>Initializes a new instance of the <see cref="MemoryBuffer"/> class from a byte array.</summary>
        /// <param name="data">Array of bytes to copy into the memory buffer.</param>
        /// <param name="name">Name of the buffer (for diagnostics).</param>
        /// <remarks>
        /// This constructor makes a copy of the data array as a <see cref="MemoryBuffer"/> the memory in the buffer
        /// is unmanaged memory usable by the LLVM native code. It is released in the Dispose method.
        /// </remarks>
        public MemoryBuffer(byte[] data, string name = "")
        {
            fixed (byte* pData = data.AsSpan())
            {
                this.BufferHandle = LLVM.CreateMemoryBufferWithMemoryRangeCopy((sbyte*)pData, (UIntPtr)data.Length, name.AsMarshaledString());
            }
        }

        /// <summary>Initializes a new instance of the <see cref="MemoryBuffer"/> class from a byte array.</summary>
        /// <param name="memoryBufferRef">An instance of the LLVMMemoryBufferRef type that will be tracked by this object.</param>
        public MemoryBuffer(LLVMMemoryBufferRef memoryBufferRef)
        {
            this.BufferHandle = memoryBufferRef;
        }

        /// <summary>Gets the size of the buffer.</summary>
        public int Size => this.BufferHandle.Handle == default ? 0 : (int)LLVM.GetBufferSize(this.BufferHandle);

        internal LLVMMemoryBufferRef BufferHandle { get; }

        /// <summary>Implicit convert to a <see cref="ReadOnlySpan{T}"/>.</summary>
        /// <param name="buffer">Buffer to convert.</param>
        /// <remarks>This is a simple wrapper around calling <see cref="Slice(int, int)"/> with default parameters.</remarks>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Named alternate exists - Slice()")]
        public static implicit operator ReadOnlySpan<byte>(MemoryBuffer buffer) => buffer.Slice(0, -1);

        /// <summary>Gets an array of bytes from the buffer.</summary>
        /// <returns>Array of bytes copied from the buffer.</returns>
        public byte[] ToArray()
        {
            if (this.BufferHandle.Handle == default)
            {
                throw new InvalidOperationException();
            }

            var bufferStart = (IntPtr)LLVM.GetBufferStart(this.BufferHandle);
            byte[] retVal = new byte[this.Size];
            Marshal.Copy(bufferStart, retVal, 0, this.Size);
            return retVal;
        }

        /// <summary>Create a <see cref="ReadOnlySpan{T}"/> for a slice of the buffer.</summary>
        /// <param name="start">Starting index for the slice [default = 0].</param>
        /// <param name="length">Length of the slice or -1 to include up to the end of the buffer [default = -1].</param>
        /// <returns>New Span.</returns>
        /// <remarks>Creates an efficient means of accessing the raw data of a buffer.</remarks>
        public ReadOnlySpan<byte> Slice(int start = 0, int length = -1)
        {
            if (this.BufferHandle.Handle == default)
            {
                throw new InvalidOperationException();
            }

            if (length == -1)
            {
                length = this.Size - start;
            }

            if ((start + length) > this.Size)
            {
                throw new ArgumentException();
            }

            unsafe
            {
                void* startSlice = LLVM.GetBufferStart(this.BufferHandle) + start;
                return new ReadOnlySpan<byte>(startSlice, length);
            }
        }

        /// <summary>Detaches the underlying buffer from automatic management.</summary>
        /// <remarks>
        /// This is used when passing the memory buffer to an LLVM object (like <see cref="Llvm.ObjectFile.TargetBinary"/>
        /// that takes ownership of the underlying buffer. Any use of the buffer after this point results in
        /// an <see cref="InvalidOperationException"/>.
        /// </remarks>
        public void Detach()
        {
            this.BufferHandle.Close();
        }
    }
}
