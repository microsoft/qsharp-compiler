// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// Adapted from https://github.com/microsoft/Quantum/blob/main/samples/chemistry/MolecularHydrogen/HydrogenSimulation.qs

namespace Microsoft.Quantum.Qir.Development {

    open QPE.Demo;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;

    @EntryPoint()
    operation RunExample() : String {

        // Add additional code here
        // for experimenting with and debugging QIR generation.

        let res = RunTrotter();
        Message($"{res}");
        return "Executed successfully!";
    }
}

namespace QPE.Demo {
    open Microsoft.Quantum.Oracles;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Chemistry;
    open Microsoft.Quantum.Chemistry.JordanWigner;
    open Microsoft.Quantum.Simulation;
    open Microsoft.Quantum.Characterization;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;


    operation AdiabaticStateEnergyUnitary(statePrepData: (Int, JordanWignerInputState[]), adiabaticUnitary : (Qubit[] => Unit), qpeUnitary : (Qubit[] => Unit is Adj + Ctl), phaseEstAlgorithm : ((DiscreteOracle, Qubit[]) => Double), qubits : Qubit[]) : Double {
        Message("invoking state prep");
        PrepareTrialState(statePrepData, qubits);
        Message("invoking adiabatic unitary");
        adiabaticUnitary(qubits);
        Message("starting passed in phase estimation alg");
        let phaseEst = phaseEstAlgorithm(OracleToDiscrete(qpeUnitary), qubits);
        Message("done with passed in phase estimation alg");
        return phaseEst;
    }

    /////////////////////

    // We can now use Canon's phase estimation algorithms to
    // learn the ground state energy using the above simulation.
    operation GetEnergyByTrotterization (qSharpData : JordanWignerEncodingData, nBitsPrecision : Int, trotterStepSize : Double, trotterOrder : Int) : (Double, Double) {

        // The data describing the Hamiltonian for all these steps is contained in
        // `qSharpData`
        let (nSpinOrbitals, fermionTermData, statePrepData, energyOffset) = qSharpData!;

        // We use a Product formula, also known as `Trotterization` to
        // simulate the Hamiltonian.
        let (nQubits, (rescaleFactor, oracle)) = TrotterStepOracle(qSharpData, trotterStepSize, trotterOrder);

        // We use the Robust Phase Estimation algorithm
        // of Kimmel, Low and Yoder.
        let phaseEstAlgorithm = RobustPhaseEstimation(nBitsPrecision, _, _);

        // This runs the quantum algorithm and returns a phase estimate.
        use qubits = Qubit[nQubits];
        Message("starting phase estimation...");
        let estPhase = AdiabaticStateEnergyUnitary(statePrepData, NoOp<Qubit[]>, oracle, phaseEstAlgorithm, qubits);
        Message("done with phase estimation...");
        ResetAll(qubits);

        // We obtain the energy estimate by rescaling the phase estimate
        // with the trotterStepSize. We also add the constant energy offset
        // to the estimated energy.
        let estEnergy = estPhase * rescaleFactor + energyOffset;

        // We return both the estimated phase, and the estimated energy.
        return (estPhase, estEnergy);
    }


    @EntryPoint()
    operation RunTrotter(): (Double, Double) {
        let hamiltonian = JWOptimizedHTerms([
                HTerm([0], [11.373844098825002]),
                HTerm([1], [11.373844098825002]),
                HTerm([2], [11.150593422875001]),
                HTerm([3], [11.150593422875001]),
            ],
            [
                HTerm([0, 1], [0.075031499775]),
                HTerm([0, 2], [0.051731125725]),
                HTerm([0, 3], [0.066373112225]),
                HTerm([1, 2], [0.066373112225]),
                HTerm([1, 3], [0.051731125725]),
                HTerm([2, 3], [0.075175674125]),
            ],
            [
                HTerm([0, 2], [0.130928605725]),
                HTerm([0, 1, 1, 2], [-0.000356462975]),
                HTerm([0, 3, 3, 2], [0.0032930437]),
                HTerm([1, 3], [0.130928605725]),
                HTerm([1, 0, 0, 3], [-0.000356462975]),
                HTerm([1, 2, 2, 3], [0.0032930437]),
            ],
            [
                HTerm([0, 1, 2, 3], [0.0, -0.0146419865, 0.0, 0.0146419865]),
            ],
        );
        Message("got the hamiltonian");
        let inputState = (2,
        [
            JordanWignerInputState((0.9937521747681269, 0.0), [0, 1]),
            JordanWignerInputState((-0.11160920725288727, 0.0), [2, 3]),
        ]
        );
        Message("created input state");
        let jWEncoded = JordanWignerEncodingData(4, hamiltonian, inputState, -667.1762402411726);

        Message("starting to run the algorithm...");
        return GetEnergyByTrotterization (jWEncoded, 10, 0.1, 1);
    }

}
