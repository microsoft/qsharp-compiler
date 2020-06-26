// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.AttributeGeneration {

    open Microsoft.Quantum.Arrays;

    function DefaultArray<'A>(size : Int) : 'A[] {
        mutable arr = new 'A[size];
        for (i in IndexRange(arr)) {
            set arr w/= i <- Default<'A>();
        }
        return arr;
    }

    operation CallDefaultArray<'A>(size : Int) : 'A[] {
        return DefaultArray<'A>(size);
    }

}

