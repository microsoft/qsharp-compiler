namespace Tets.Samples.BernVaz {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Convert;

    @EntryPoint()
    operation BernsteinVaziraniTestCase (nQubits : Int, patternInt : Int) : Result[] {
        let pattern = IntAsBoolArray(patternInt, nQubits);
        return ParityViaFourierSampling(ParityOperation(pattern), nQubits);
    }

    // To demonstrate the Bernstein–Vazirani algorithm, we define
    // a function which returns black-box operations (Qubit[] => Unit) of
    // the form

    //    U_f |𝑥〉|𝑦〉 = |𝑥〉|𝑦 ⊕ 𝑓(𝑥)〉,

    // as described above.

    // In particular, we define 𝑓 by providing the pattern 𝑟⃗. Thus, we can
    // easily assert that the pattern measured by the Bernstein–Vazirani
    // algorithm matches the pattern we used to define 𝑓.

    // As is idiomatic in Q#, we define an operation that we will typically
    // only call by partially applying it from within a matching function.
    // To indicate that we are using this idiom, we mark the operation
    // as internal, and provide documentation comments for the function itself.
    internal operation ParityOperationImpl(pattern : Bool[], queryRegister : Qubit[], target : Qubit) : Unit {
        if (Length(queryRegister) != Length(pattern)) {
            fail "Length of input register must be equal to the pattern length.";
        }

        for ((patternBit, controlQubit) in Zip(pattern, queryRegister)) {
            if (patternBit) {
                Controlled X([controlQubit], target);
            }
        }
    }

    /// # Summary
    /// Given a bitstring 𝑟⃗ = (r₀, …, rₙ₋₁), returns an operation implementing
    /// a unitary 𝑈 that acts on 𝑛 + 1 qubits as
    ///
    ///       𝑈 |𝑥〉|𝑦〉 = |𝑥〉|𝑦 ⊕ 𝑓(𝑥)〉,
    /// where 𝑓(𝑥) = Σᵢ 𝑥ᵢ 𝑟ᵢ mod 2.
    ///
    /// # Input
    /// ## pattern
    /// The bitstring 𝑟⃗ used to define the function 𝑓.
    ///
    /// # Output
    /// An operation implementing 𝑈.
    function ParityOperation(pattern : Bool[]) : ((Qubit[], Qubit) => Unit) {
        return ParityOperationImpl(pattern, _, _);
    }

    /// # Summary
    /// ParityViaFourierSampling implements the Bernstein-Vazirani quantum algorithm.
    /// This algorithm computes for a given Boolean function that is promised to be
    /// a parity 𝑓(𝑥₀, …, 𝑥ₙ₋₁) = Σᵢ 𝑟ᵢ 𝑥ᵢ a result in form of
    /// a bit vector (𝑟₀, …, 𝑟ₙ₋₁) corresponding to the parity function.
    /// Note that it is promised that the function is actually a parity function.
    ///
    /// # Input
    /// ## Uf
    /// A quantum operation that implements |𝑥〉|𝑦〉 ↦ |𝑥〉|𝑦 ⊕ 𝑓(𝑥)〉,
    /// where 𝑓 is a Boolean function that implements a parity Σᵢ 𝑟ᵢ 𝑥ᵢ.
    /// ## n
    /// The number of bits of the input register |𝑥〉.
    ///
    /// # Output
    /// An array of type `Result[]` that contains the parity 𝑟⃗ = (𝑟₀, …, 𝑟ₙ₋₁).
    ///
    /// # See Also
    /// - For details see Section 1.4.3 of Nielsen & Chuang.
    ///
    /// # References
    /// - [ *Ethan Bernstein and Umesh Vazirani*,
    ///     SIAM J. Comput., 26(5), 1411–1473, 1997 ](https://doi.org/10.1137/S0097539796300921)
    operation ParityViaFourierSampling(Uf : ((Qubit[], Qubit) => Unit), n : Int) : Result[] {
        // Now, we allocate n + 1 clean qubits. Note that the function Uf is defined
        // on inputs of the form (x, y), where x has n bits and y has 1 bit.
        using ((queryRegister, target) = (Qubit[n], Qubit())) {
            // The last qubit needs to be flipped so that the function will
            // actually be computed into the phase when Uf is applied.
            X(target);

            within {
                // Now, a Hadamard transform is applied to each of the qubits.
                // As the last step before the measurement, a Hadamard transform is
                // applied to all qubits except last one. We could apply the transform to
                // the last qubit also, but this would not affect the final outcome.
                // We use a within-apply block to ensure that the Hadmard transform is
                // correctly inverted.
                ApplyToEachA(H, queryRegister);
            } apply {
                H(target);
                // We now apply Uf to the n+1 qubits, computing |x, y〉 ↦ |x, y ⊕ f(x)〉.
                Uf(queryRegister, target);
            }

            // Finally, measure and reset all qubits, returning the results.
            return ForEach(MResetZ, queryRegister);
        }
    }
}