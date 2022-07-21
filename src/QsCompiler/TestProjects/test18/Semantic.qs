function getI(): Complex {
    return 3; // QS0001: Expected type __NotebookNamespace__.Complex, but actual type was Int.
}

function getI(): Complex { // QS6001: Invalid callable declaration. A function or operation with the name "getI" already exists.
    return (1.570796, 1.570796);
}

function trolling(): Unit {
    let i = getI(true); // QS0001: Expected type Unit, but actual type was Bool.
    let alpha = i::Imaginary;
    mutable oops = false;
    set oops = alpha; // QS0001: Expected type Bool, but actual type was Double.
}
