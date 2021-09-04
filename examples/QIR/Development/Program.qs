namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Preparation;
    open Microsoft.Quantum.Characterization;
    open Microsoft.Quantum.Logical;
    open Microsoft.Quantum.Samples;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arithmetic;
    open Microsoft.Quantum.MachineLearning;

    internal function StatePreparationSBMComputeCoefficients(coefficients : ComplexPolar[]) : (Double[], Double[], ComplexPolar[]) {
        mutable disentanglingZ = new Double[Length(coefficients) / 2];
        mutable disentanglingY = new Double[Length(coefficients) / 2];
        mutable newCoefficients = new ComplexPolar[Length(coefficients) / 2];

        for idxCoeff in 0 .. 2 .. Length(coefficients) - 1 {
            let (rt, phi, theta) = BlochSphereCoordinates(coefficients[idxCoeff], coefficients[idxCoeff + 1]);
            set disentanglingZ w/= idxCoeff / 2 <- 0.5 * phi;
            set disentanglingY w/= idxCoeff / 2 <- 0.5 * theta;
            set newCoefficients w/= idxCoeff / 2 <- rt;
        }

        return (disentanglingY, disentanglingZ, newCoefficients);
    }

    internal function ApproximatelyUnprepareArbitraryStatePlan(
        tolerance : Double, coefficients : ComplexPolar[],
        (rngControl : Range, idxTarget : Int)
    )
    : (Qubit[] => Unit is Adj + Ctl)[] {
        mutable plan = new (Qubit[] => Unit is Adj + Ctl)[0];

        let (disentanglingY, disentanglingZ, newCoefficients) = StatePreparationSBMComputeCoefficients(coefficients);
        return plan;
    }

    internal operation ApplyToLittleEndian(bareOp : ((Qubit[]) => Unit is Adj + Ctl), register : LittleEndian)
    : Unit is Adj + Ctl {
        bareOp(register!);
    }

    function _CompileApproximateArbitraryStatePreparation(
        tolerance : Double,
        coefficients : ComplexPolar[],
        nQubits : Int
    )
    : (LittleEndian => Unit is Adj + Ctl) {
        let coefficientsPadded = Padded(-2 ^ nQubits, ComplexPolar(0.0, 0.0), coefficients);
        let plan = ApproximatelyUnprepareArbitraryStatePlan(
            tolerance, coefficientsPadded, (0..0, 0)
        );
        let unprepare = BoundCA(plan);
        return ApplyToLittleEndian(Adjoint unprepare, _);
    }

    function ApproximateInputEncoder()
    : StateGenerator {

        let nrQs = 5; // fails if this is 0 ...
        return StateGenerator(
            nrQs,
            _CompileApproximateArbitraryStatePreparation(0.1, [], nrQs)
        );
    }

    internal function _EncodeSample(sample : LabeledSample)
    : (LabeledSample, StateGenerator) {
        return (
            sample,
            ApproximateInputEncoder()
        );
    }

    @EntryPoint()
    operation RunExample() : String {

        let sample = LabeledSample([], 1);
        let _ = _EncodeSample(sample);
        return "Executed successfully!";
    }
}
