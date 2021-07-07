// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum {

    newtype Pair = (Fst : Int, Snd : Int);
    newtype NestedPair = (Double, ((Fst : Bool, String), Snd : Int));
}