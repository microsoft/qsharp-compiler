// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum 
{
    newtype Pair = (Int,Int);
    newtype Unused = (Int,Int);
    function emptyFunction (p:Pair) : Unit { body intrinsic; }
    operation emptyOperation () : Unit { body intrinsic; }
}