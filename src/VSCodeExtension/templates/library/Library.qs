﻿namespace <%= name %> {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;


    operation SayHello() : Unit {
        Message("Hello quantum world!");
    }
}
