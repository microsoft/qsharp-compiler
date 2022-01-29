// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;

namespace LlvmBindings.Interop
{
    public unsafe struct MarshaledStringArray : IDisposable
    {
        public MarshaledStringArray(ReadOnlySpan<string> inputs)
        {
            if (inputs.IsEmpty)
            {
                this.Count = 0;
                this.Values = null;
            }
            else
            {
                this.Count = inputs.Length;
                this.Values = new MarshaledString[this.Count];

                for (int i = 0; i < this.Count; i++)
                {
                    this.Values[i] = new MarshaledString(inputs[i].AsSpan());
                }
            }
        }

        public int Count { get; private set; }

        public MarshaledString[]? Values { get; private set; }

        public void Dispose()
        {
            if (this.Values != null)
            {
                for (int i = 0; i < this.Values.Length; i++)
                {
                    this.Values[i].Dispose();
                }

                this.Values = null;
                this.Count = 0;
            }
        }

        public void Fill(sbyte** pDestination)
        {
            if (this.Values != null)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    pDestination[i] = this.Values[i];
                }
            }
        }
    }
}
