// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.InteropServices;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMMCJITCompilerOptions
    {
        public static LLVMMCJITCompilerOptions Create()
        {
            LLVMMCJITCompilerOptions options;
            LLVM.InitializeMCJITCompilerOptions(&options, (UIntPtr)Marshal.SizeOf<LLVMMCJITCompilerOptions>());
            return options;
        }
    }
}
