// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace Microsoft.Quantum.Simulation.QuantumProcessor.Extensions {
    operation ApplyIfZero<'T>(measurementResult : Result, (onResultZeroOp : ('T => Unit), zeroArg : 'T)) : Unit { }
    operation ApplyIfZeroA<'T>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Adj), zeroArg : 'T)) : Unit is Adj { }
    operation ApplyIfZeroC<'T>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Ctl), zeroArg : 'T)) : Unit is Ctl { }
    operation ApplyIfZeroCA<'T>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Ctl + Adj), zeroArg : 'T)) : Unit is Ctl + Adj { }

    operation ApplyIfOne<'T>(measurementResult : Result, (onResultOneOp : ('T => Unit), oneArg : 'T)) : Unit { }
    operation ApplyIfOneA<'T>(measurementResult : Result, (onResultOneOp : ('T => Unit is Adj), oneArg : 'T)) : Unit is Adj { }
    operation ApplyIfOneC<'T>(measurementResult : Result, (onResultOneOp : ('T => Unit is Ctl), oneArg : 'T)) : Unit is Ctl { }
    operation ApplyIfOneCA<'T>(measurementResult : Result, (onResultOneOp : ('T => Unit is Ctl + Adj), oneArg : 'T)) : Unit is Ctl + Adj { }

    operation ApplyIfElseR<'T,'U>(measurementResult : Result, (onResultZeroOp : ('T => Unit), zeroArg : 'T) , (onResultOneOp : ('U => Unit), oneArg : 'U)) : Unit { }
    operation ApplyIfElseRA<'T,'U>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Adj), zeroArg : 'T) , (onResultOneOp : ('U => Unit is Adj), oneArg : 'U)) : Unit is Adj { }
    operation ApplyIfElseRC<'T,'U>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Ctl), zeroArg : 'T) , (onResultOneOp : ('U => Unit is Ctl), oneArg : 'U)) : Unit is Ctl { }
    operation ApplyIfElseCA<'T,'U>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Adj + Ctl), zeroArg : 'T) , (onResultOneOp : ('U => Unit is Adj + Ctl), oneArg : 'U)) : Unit is Ctl + Adj { }
}