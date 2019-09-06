// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Targeting

type TargetCapabilities =
    {
        CanDoIf : bool
        CanDoRepeat : bool
        CanMeasureAndContinue : bool
        CanFail : bool
        CanMessage : bool
        CanCompute : bool
    }
