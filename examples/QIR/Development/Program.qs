namespace Microsoft.Quantum.Qir.Development {

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

    @EntryPoint()
    operation RunExample() : String {

        let result = TrainingSample();
        Message($"Result: {result}");
        return "Executed successfully!";
    }

    function WithProductKernel(scale : Double, sample : Double[]) : Double[] {
        //return sample; // works
        return sample + [scale]; // doesn't work...
    }

    function Preprocessed(samples : Double[][]) : Double[][] {
        let scale = 1.0;

        return Mapped(
            WithProductKernel(scale, _),
            samples
        );
    }

    function ClassifierStructure() : ControlledRotation[] { // some lines could be commented out here, some couldn't
        return [
            ControlledRotation((0, new Int[0]), PauliX, 4),
            ControlledRotation((0, new Int[0]), PauliZ, 5),
            ControlledRotation((0, [1]), PauliX, 0)
        ];
    }

    operation EstimateClassificationProbability(
        tolerance : Double,
        model : SequentialModel,
        sample : Double[],
        nMeasurements: Int
    )
    : Double {
        Message($"starting EstimateClassificationProbability");
        // COMMENTING THIS OUT MAKE IT WORK TOO
        let encodedSample = ApproximateInputEncoder(tolerance / IntAsDouble(Length(model::Structure)), sample);
        Message($"done with EstimateClassificationProbability");
        return 1.;
    }

    operation TrainingSample() : (Double[], Double) {
        let features = Features();
        let labels = [1];
        let starting_points = [[0.5]];

        let samples = Mapped(
            LabeledSample,
            Zipped(Preprocessed(features), labels) // removing preprocessed here seems to avoid the issue
        );

        let model = SequentialModel(ClassifierStructure(), [0.1], 0.0);
        let options = DefaultTrainingOptions();

        let effectiveTolerance =
            IsEmpty(model::Structure)
            ? options::Tolerance
            | options::Tolerance / IntAsDouble(Length(model::Structure));

        let ret = ForEach(
            EstimateClassificationProbability(
                effectiveTolerance, model, _, options::NMeasurements
            ),
            Mapped(_Features, samples)
        );

        return (model::Parameters, model::Bias);
    }
}
