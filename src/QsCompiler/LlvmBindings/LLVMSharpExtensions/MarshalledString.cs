// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LLVMSharp.Interop
{
    internal unsafe struct MarshaledString : IDisposable
    {
        public MarshaledString(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty)
            {
                var value = Marshal.AllocHGlobal(1);
                Marshal.WriteByte(value, 0, 0);

                this.Length = 0;
                this.Value = (sbyte*)value;
            }
            else
            {
                var valueBytes = Encoding.UTF8.GetBytes(input.ToString());
                var length = valueBytes.Length;
                var value = Marshal.AllocHGlobal(length + 1);
                Marshal.Copy(valueBytes, 0, value, length);
                Marshal.WriteByte(value, length, 0);

                this.Length = length;
                this.Value = (sbyte*)value;
            }
        }

        public int Length { get; private set; }

        public sbyte* Value { get; private set; }

        public void Dispose()
        {
            if (this.Value != default)
            {
                Marshal.FreeHGlobal((IntPtr)this.Value);
                this.Value = default;
                this.Length = 0;
            }
        }

        public static implicit operator sbyte*(in MarshaledString value)
        {
            return value.Value;
        }

        public override string ToString()
        {
            var span = new ReadOnlySpan<byte>(this.Value, this.Length);
            return span.AsString();
        }
    }
}
