namespace Microsoft.Quantum.Samples.Chemistry.SimpleVQE.EstimateEnergy {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Chemistry.JordanWigner;

    /// # Summary
    /// Computes the energy associated to a given Jordan-Wigner Hamiltonian term
    ///
    /// # Description
    /// This operation estimates the expectation value associated to each measurement operator and
    /// multiplies it by the corresponding coefficient, using sampling.
    /// The results are aggregated into a variable containing the energy of the Jordan-Wigner term.
    ///
    /// # Input
    /// ## inputState
    /// The terms to create a unitary used for state preparation.
    /// ## ops
    /// The measurement operators of the Jordan-Wigner term.
    /// ## coeffs
    /// The coefficients of the Jordan-Wigner term.
    /// ## nQubits
    /// The number of qubits required to simulate the molecular system.
    /// ## nSamples
    /// The number of samples to use for the estimation of the term expectation.
    ///
    /// # Output
    /// The energy associated to the Jordan-Wigner term.
    operation SumTermExpectation(inputState: (Int, JordanWignerInputState[]), ops : Pauli[][], coeffs : Double[], nQubits : Int,  nSamples : Int) : Double {
        mutable jwTermEnergy = 0.;
        for (i in 0..Length(coeffs)-1) {
            let coeff = coeffs[i];
            let op = ops[i];
            // Only perform computation if the coefficient is significant enough
            if (coeff >= 1e-10 or coeff <= -1e-10) {
                // Compute expectation value using the fast frequency estimator, add contribution to Jordan-Wigner term energy
                let termExpectation = TermExpectation(inputState, op, nQubits, nSamples);
                set jwTermEnergy += (2. * termExpectation - 1.) * coeff;
            }
        }

        return jwTermEnergy;
    }

    operation JointMeasure(ops : Pauli[], qbs : Qubit[]) : Result
    {
        using (aux = Qubit())
        {
            // aux starts in the Z=|0> state
            for (i in 0..Length(qbs)-1)
            {
                let op = ops[i];
                let qb = qbs[i];
                if (op == PauliX)
                {
                    H(qb);
                    CNOT(qb, aux);
                    H(qb);
                }
                elif (op == PauliY)
                {
                    S(qb);
                    H(qb);
                    CNOT(qb, aux);
                    H(qb);
                    S(qb);
                    Z(qb);
                }
                elif (op == PauliZ)
                {
                    CNOT(qb, aux);
                }
                // Ignore PauliI
            }

            return M(aux);
        }
    }

    /// # Summary
    /// Given a preparation and measurement, estimates the frequency
    /// with which that measurement succeeds (returns `Zero`) by
    /// performing a given number of trials.
    ///
    /// # Input
    /// ## inputState
    /// The terms to create a unitary used for state preparation.
    /// ## measOp
    /// A list of operations representing the measurement of interest.
    /// ## nQubits
    /// The number of qubits on which the preparation and measurement
    /// each act.
    /// ## nSamples
    /// The number of times that the measurement should be performed
    /// in order to estimate the frequency of interest.
    ///
    /// # Output
    /// An estimate $\hat{p}$ of the frequency with which
    /// $M(P(\ket{00 \cdots 0}\bra{00 \cdots 0}))$ returns `Zero`,
    /// obtained using the unbiased binomial estimator $\hat{p} =
    /// n\_{\uparrow} / n\_{\text{measurements}}$, where $n\_{\uparrow}$ is
    /// the number of `Zero` results observed.
    ///
    /// This is particularly important on target machines which respect
    /// physical limitations, such that probabilities cannot be measured.
    operation TermExpectation(
        inputState: (Int, JordanWignerInputState[]),
        measOp: Pauli[],
        nQubits: Int,
        nSamples: Int
    ) : Double {
        mutable nUp = 0;
        for (idxMeasurement in 0 .. nSamples - 1) {
            using (register = Qubit[nQubits]) {
                PrepareTrialState(inputState, register);
                let result = JointMeasure(measOp, register);
                if (result == Zero) {
                    set nUp += 1;
                }
                for (q in register)
                {
                    let r = M(q);
                }
            }
        }
        return IntAsDouble(nUp) / IntAsDouble(nSamples);
    }
}
