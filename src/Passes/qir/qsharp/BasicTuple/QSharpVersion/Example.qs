namespace Microsoft.Quantum.Samples {
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Convert;


    // # Notes
    //
    // ## Encoder
    // To find the encoder in Python:
    // ```python
    // import qecc as q
    // stab = q.StabilizerCode(
    //     group_generators=[
    //         "XZIZXII",
    //         "IXZIZXI",
    //         "IIXZIZX",
    //         "XIIXZIZ",
    //         "ZXIIXZI",
    //         "IZXIIXZ",
    //     ],
    //     logical_xs=[
    //         "X" * 7
    //     ],
    //     logical_zs=[
    //         "Z" * 7
    //     ]
    // )
    // ```
    //
    // We get the following decomposition in qcviewer notation:
    //
    // ```
    //     CNOT    2 0
    //     CNOT    5 0
    //     CNOT    3 1
    //     CNOT    4 1
    //     CNOT    5 1
    //     CNOT    6 1
    //     CNOT    3 2
    //     CNOT    5 3
    //     CNOT    6 4
    //     H       0
    //     H       1
    //     H       2
    //     H       3
    //     H       4
    //     H       5
    //     H       6
    //     R_pi4    1
    //     CZ      1 2
    //     CZ      1 3
    //     CZ      0 4
    //     CZ      1 4
    //     CZ      2 4
    //     CZ      1 5
    //     CZ      2 5
    //     CZ      2 6
    //     CZ      5 6
    //     H       0
    //     H       1
    //     H       2
    //     H       3
    //     H       4
    //     H       5
    //     H       6
    //     H       6
    //     SWAP    5 6
    //     H       6
    //     CZ      4 6
    //     CNOT    4 5
    //     CZ      3 4
    //     R_pi4    3
    //     CNOT    1 6
    //     CNOT    1 4
    //     CNOT    1 2
    //     CNOT    0 6
    //     CNOT    0 5
    //     CNOT    0 4
    //     CNOT    0 3
    //     CNOT    0 2
    //     CNOT    0 1
    //     Z       1
    //     X       2
    //     Z       3
    //     Z       4
    //     Z       5
    //     X       6
    // ```

    @EntryPoint()
    operation RunMain() : Unit {
        let generators = CyclicCodeGenerators();
        let errorTable = PhysicalErrorsBySyndrome(generators);

        // Initialize prior distributions over prX and prZ.
        // Since our likelihood is the product of two binomials,
        // beta distributions form conjugate priors for prZ and prZ.
        // Thankfully, beta distributions have a nice parameterization as
        //
        //     π = Beta(α, β)
        //     α = # of successes seen
        //     β = # of failures seen
        //
        // where "success" and "failure" are interpreted here as "an error
        // of that type occured on a single qubit" and the negation thereof,
        // respectively.
        //
        // In the special case that our priors only assume integer numbers of
        // events, we then can represent πx and πz as (Int, Int), adding the
        // x and z weights of each MAP error as we go to update our priors.
        //
        // We initialize priors to assume 1% error on each qubit.
        mutable priorPrX = (1, 99);
        mutable priorPrZ = (1, 99);

        use logicalQubit = Qubit();
        use s0 = Qubit();
        use s1 = Qubit();
        use s2 = Qubit();
        use s3 = Qubit();
        use s4 = Qubit();
        use s5 = Qubit();
        let syndromeQubits = [s0, s1, s2, s3, s4, s5];
        let register = [logicalQubit] + syndromeQubits;

        Encode(register);
        for _ in 1..10 {
            // Apply some easily corrected error.
            X(register[0]);
            set (priorPrX, priorPrZ) = Recover(
                generators,
                errorTable,
                (priorPrX, priorPrZ),
                register
            );
        }
        Adjoint Encode(register);
        DumpRegister((), register);

        ResetAll(register);
    }

    function BetaMean(nSuccesses : Int, nFailures : Int) : Double {
        let α = IntAsDouble(nSuccesses);
        let β = IntAsDouble(nFailures);
        return α / (α + β);
    }

    // ApplyToEachCA(CNOT(_, target), controls);
    operation ApplyCnotToEachControl(
        controls : Qubit[],
        target : Qubit
    ) : Unit is Adj + Ctl {
        for control in controls {
            CNOT(control, target);
        }
    }

    // ApplyToEachCA(CNOT(control, targets), targets);
    operation ApplyCnotToEachTarget(
        control : Qubit,
        targets : Qubit[]
    ) : Unit is Adj + Ctl {
        for target in targets {
            CNOT(control, target);
        }
    }

    operation ApplyHToEach(targets : Qubit[]) : Unit is Adj + Ctl {
        for target in targets {
            H(target);
        }
    }

    operation CZ(control : Qubit, target : Qubit) : Unit is Adj + Ctl {
        Controlled Z([control], target);
    }

    operation Encode(qs : Qubit[]) : Unit is Adj + Ctl {
        // See above comments as to why this is the right encoder.
        ApplyCnotToEachControl([qs[2], qs[5]], qs[0]);
        ApplyCnotToEachControl(qs[3..6], qs[1]);
        CNOT(qs[3], qs[2]);
        CNOT(qs[5], qs[3]);
        CNOT(qs[6], qs[4]);
        ApplyHToEach(qs);
        S(qs[1]);
        CZ(qs[1], qs[2]);
        CZ(qs[1], qs[3]);
        CZ(qs[0], qs[4]);
        CZ(qs[1], qs[4]);
        CZ(qs[2], qs[4]);
        CZ(qs[1], qs[5]);
        CZ(qs[2], qs[5]);
        CZ(qs[2], qs[6]);
        CZ(qs[5], qs[6]);
        ApplyHToEach(qs);
        H(qs[6]);
        SWAP(qs[5], qs[6]);
        H(qs[6]);
        CZ(qs[4], qs[6]);
        CNOT(qs[4], qs[5]);
        CZ(qs[3], qs[4]);
        S(qs[3]);
        CNOT(qs[1], qs[6]);
        CNOT(qs[1], qs[4]);
        CNOT(qs[1], qs[2]);
        ApplyCnotToEachTarget(qs[0], qs[6..-1..1]);
        Z(qs[1]);
        X(qs[2]);
        Z(qs[3]);
        Z(qs[4]);
        Z(qs[5]);
        X(qs[6]);
    }

    operation ApplyPauli(pauli : Pauli[], register : Qubit[]) : Unit is Adj + Ctl {
        for idx in 0..Length(register) - 1 {
            let p = pauli[idx];
            if p == PauliX {
                X(register[idx]);
            } elif p == PauliY {
                Y(register[idx]);
            } elif p == PauliZ {
                Z(register[idx]);
            }
        }
    }

    function MaximumAPosteriError(
        potentialErrors : (Int, Int, Pauli[])[],
        (estPrX : Double, estPrZ : Double)
    ) : (Double, Pauli[]) {
        // Cycle through all possible errors consistent with our observed
        // syndrome and pick the single most probable (maximum a posteri, or
        // MAP).
        let (_, _, potentialError) = potentialErrors[0];
        let nQubits = Length(potentialErrors);
        mutable maxAP = (0.0, [PauliI, size=nQubits]);
        for (xWt, zWt, error) in potentialErrors {
            let prError = PowD(estPrX, IntAsDouble(xWt)) *
                          PowD(estPrZ, IntAsDouble(zWt)) *
                          PowD(1.0 - estPrX - estPrZ, IntAsDouble(nQubits - xWt - zWt));
            let (bestPr, bestError) = maxAP;
            if prError > bestPr {
                set maxAP = (prError, error);
            }
        }
        return maxAP;
    }

    function BetaUpdated((nSuccesses : Int, nFailures : Int), (nNewSuccesses : Int, nNewFailures : Int)) : (Int, Int) {
        return (nSuccesses + nNewSuccesses, nFailures + nNewFailures);
    }

    operation Recover(
        stabilizerGenerators : Pauli[][],
        errorTable : (Int, Int, Pauli[])[][],
        (priorPrX : (Int, Int), priorPrZ : (Int, Int)),
        register : Qubit[]
    ) : ((Int, Int), (Int, Int)) {
        let syndrome = MeasureSyndrome(stabilizerGenerators, register);
        let potentialErrors = errorTable[syndrome];

        // Apply the MAP error.
        let estPrX = BetaMean(priorPrX);
        let estPrZ = BetaMean(priorPrZ);
        Message($"Pr(X) = {estPrX}, Pr(Z) = {estPrZ}");
        let (prAtMap, maxApError) = MaximumAPosteriError(potentialErrors, (estPrX, estPrZ));
        Message($"Pr(error = {maxApError} | syndrome = {syndrome}) = {prAtMap}.");
        ApplyPauli(maxApError, register);

        let (xWt, zWt) = Weights(maxApError);
        let nQubits = Length(maxApError);
        return (
            BetaUpdated(priorPrX, (xWt, nQubits - xWt)),
            BetaUpdated(priorPrZ, (zWt, nQubits - zWt))
        );
    }

    operation MeasureSyndrome(stabilizerGenerators : Pauli[][], register : Qubit[]) : Int {
        mutable syndrome = [false, size=Length(stabilizerGenerators)];
        for idx in 0..Length(syndrome) - 1 {
            let r = Measure(stabilizerGenerators[idx], register);
            if r == One {
                set syndrome w/= idx <- true;
            }
        }
        return BoolArrayAsInt(syndrome);
    }

    function IntAsPauliArray(nQubits : Int, idxPauli : Int) : Pauli[] {
        // 𝑥₀𝑧₀𝑥₁𝑧₁…𝑥ₙ₋₁𝑧ₙ₋₁
        let bits = IntAsBoolArray(idxPauli, 2 * nQubits);
        let xs = bits[0..2...];
        let zs = bits[1..2...];
        mutable paulis = [PauliI, size=nQubits];
        for idx in 0..nQubits - 1 {
            set paulis w/= idx <-
                not xs[idx] and not zs[idx] ? PauliI |
                not xs[idx] and     zs[idx] ? PauliZ |
                    xs[idx] and not zs[idx] ? PauliX |
                                              PauliY;
        }
        return paulis;
    }

    function InnerProductP(left : Pauli[], right : Pauli[]) : Bool {
        mutable result = false;
        for idx in 0..Length(left) - 1 {
            let l = left[idx];
            let r = right[idx];
            if l != PauliI and r != PauliI and l != r {
                set result = not result;
            }
        }
        return result;
    }

    function SyndromeForError(stabilizerGenerators : Pauli[][], error : Pauli[]) : Int {
        let nQubits = Length(error);
        mutable syndrome = [false, size=Length(stabilizerGenerators)];
        for idxGenerator in 0..Length(stabilizerGenerators) - 1 {
            set syndrome w/= idxGenerator <- InnerProductP(stabilizerGenerators[idxGenerator], error);
        }
        return BoolArrayAsInt(syndrome);
    }

    function CyclicCodeGenerators() : Pauli[][] {
        return [
            [PauliX, PauliZ, PauliI, PauliZ, PauliX, PauliI, PauliI],
            [PauliI, PauliX, PauliZ, PauliI, PauliZ, PauliX, PauliI],
            [PauliI, PauliI, PauliX, PauliZ, PauliI, PauliZ, PauliX],
            [PauliX, PauliI, PauliI, PauliX, PauliZ, PauliI, PauliZ],
            [PauliZ, PauliX, PauliI, PauliI, PauliX, PauliZ, PauliI],
            [PauliI, PauliZ, PauliX, PauliI, PauliI, PauliX, PauliZ]
        ];
    }

    function Weights(pauli : Pauli[]) : (Int, Int) {
        mutable xWt = 0;
        mutable zWt = 0;
        for p in pauli {
            if p == PauliX or p == PauliY {
                set xWt += 1;
            }
            if p == PauliZ or p == PauliY {
                set zWt += 1;
            }
        }
        return (xWt, zWt);
    }

    /// # Output
    /// The possible physical errors that can occur on a given system,
    /// grouped by their syndrome (as represented by a little-endian encoding).
    function PhysicalErrorsBySyndrome(stabilizerGenerators : Pauli[][]) : (Int, Int, Pauli[])[][] {
        let nQubits = Length(stabilizerGenerators[0]);
        let nLogical = nQubits - Length(stabilizerGenerators);
        let nSyndromes = 2^(nQubits - nLogical);
        let nErrors = 4^nQubits;
        mutable errors = [[], size=nSyndromes];
        for idxError in 0..nErrors - 1 {
            let error = IntAsPauliArray(nQubits, idxError);
            let (xWt, zWt) = Weights(error);
            let syndrome = SyndromeForError(stabilizerGenerators, error);
            let errorsAtSyndrome = errors[syndrome];
            set errors w/= syndrome <- errorsAtSyndrome + [(xWt, zWt, error)];
        }
        return errors;
    }
}