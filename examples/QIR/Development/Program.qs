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
        return sample + [scale * Fold(TimesD, 1.0, sample)];
    }

    function Preprocessed(samples : Double[][]) : Double[][] {
        let scale = 1.0;

        return Mapped(
            WithProductKernel(scale, _),
            samples
        );
    }

    function DefaultSchedule(samples : Double[][]) : SamplingSchedule {
        return SamplingSchedule([
            0..Length(samples) - 1
        ]);
    }

    function ClassifierStructure() : ControlledRotation[] {
        return [
            ControlledRotation((0, new Int[0]), PauliX, 4),
            ControlledRotation((0, new Int[0]), PauliZ, 5),
            ControlledRotation((1, new Int[0]), PauliX, 6),
            ControlledRotation((1, new Int[0]), PauliZ, 7),
            ControlledRotation((0, [1]), PauliX, 0),
            ControlledRotation((1, [0]), PauliX, 1),
            ControlledRotation((1, new Int[0]), PauliZ, 2),
            ControlledRotation((1, new Int[0]), PauliX, 3)
        ];
    }

    operation TrainHalfMoonModel(
        trainingVectors : Double[][],
        trainingLabels : Int[],
        initialParameters : Double[][]
    ) : (Double[], Double) {
        let samples = Mapped(
            LabeledSample,
            Zipped(Preprocessed(trainingVectors), trainingLabels)
        );
        Message("Ready to train.");
        let (optimizedModel, nMisses) = TrainSequentialClassifier(
            Mapped(
                SequentialModel(ClassifierStructure(), _, 0.0),
                initialParameters
            ),
            samples,
            DefaultTrainingOptions()
                w/ LearningRate <- 0.1
                w/ MinibatchSize <- 15
                w/ Tolerance <- 0.005
                w/ NMeasurements <- 10000
                w/ MaxEpochs <- 16
                w/ VerboseMessage <- Message,
            DefaultSchedule(trainingVectors),
            DefaultSchedule(trainingVectors)
        );
        Message($"Training complete, found optimal parameters: {optimizedModel::Parameters}");
        return (optimizedModel::Parameters, optimizedModel::Bias);
    }

    operation TrainSequentialClassifierAtModel(
        model : SequentialModel,
        samples : LabeledSample[],
        options : TrainingOptions,
        trainingSchedule : SamplingSchedule,
        validationSchedule : SamplingSchedule
    )
    : (SequentialModel, Int) {
        //return (model, 1); // FIXME: THIS RESULTS IN A POTENTIALLY LEAKED OBJECT

        Message($"starting TrainSequentialClassifierAtModel");
        let optimizedModel = _TrainSequentialClassifierAtModel(model, samples, options, trainingSchedule);
        Message($"got optimized model");

        return (optimizedModel, 1);
    }

    operation EstimateFrequency (preparation : (Qubit[] => Unit), measurement : (Qubit[] => Result), nQubits : Int, nMeasurements : Int) : Double
    {
        Message($"starting EstimateFrequency");
        mutable nUp = 0;

        for idxMeasurement in 0 .. nMeasurements - 1 {
            use register = Qubit[nQubits];
            preparation(register);
            let result = measurement(register);

            if (result == Zero) {
                // NB!!!!! This reverses Zero and One to use conventions
                //         common in the QCVV community. That is confusing
                //         but is confusing with an actual purpose.
                set nUp = nUp + 1;
            }

            // NB: We absolutely must reset here, since preparation()
            //     and measurement() can each use randomness internally.
            ApplyToEach(Reset, register);
        }

        Message($"done with EstimateFrequency");
        return IntAsDouble(nUp) / IntAsDouble(nMeasurements);
    }

    operation EstimateFrequencyA (preparation : (Qubit[] => Unit is Adj), measurement : (Qubit[] => Result), nQubits : Int, nMeasurements : Int) : Double
    {
        Message($"starting EstimateFrequencyA");
        return EstimateFrequency(preparation, measurement, nQubits, nMeasurements);
    }

    internal operation _TrainSequentialClassifierAtModel(
        model : SequentialModel,
        samples : LabeledSample[],
        options : TrainingOptions,
        schedule : SamplingSchedule
    )
    : SequentialModel {

        let nSamples = Length(samples);
        let features = Mapped(_Features, samples);
        let actualLabels = Mapped(_Label, samples);

        let probabilities = [1., size = Length(features)];
        Message($"got probabilities");

        mutable bestSoFar = model
            w/ Bias <- _UpdatedBias(
                Zipped(probabilities, actualLabels),
                model::Bias, options::Tolerance
            );
        let inferredLabels = InferredLabels(
            bestSoFar::Bias, probabilities
        );
        mutable nBestMisses = Length(
            Misclassifications(inferredLabels, actualLabels)
        );
        mutable current = bestSoFar;

        // Encode samples first.
        options::VerboseMessage("    Pre-encoding samples...");
        let effectiveTolerance = options::Tolerance / IntAsDouble(Length(model::Structure));
        let nQubits = MaxI(FeatureRegisterSize(samples[0]::Features), NQubitsRequired(model));
        let encodedSamples = Mapped(_EncodeSample(effectiveTolerance, nQubits, _), samples);

        return bestSoFar;
    }

    operation TrainSequentialClassifier(
        models : SequentialModel[],
        samples : LabeledSample[],
        options : TrainingOptions,
        trainingSchedule : SamplingSchedule,
        validationSchedule : SamplingSchedule
    ) : (SequentialModel, Int) {
        mutable bestSoFar = Default<SequentialModel>()
                            w/ Structure <- (Head(models))::Structure;
        mutable bestValidation = Length(samples) + 1;

        let features = Mapped(_Features, samples);
        let labels = Mapped(_Label, samples);

        for (idxModel, model) in Enumerated(models) {
            options::VerboseMessage($"  Beginning training at start point #{idxModel}...");
            let (proposedUpdate, localMisses) = TrainSequentialClassifierAtModel(
                model,
                samples, options, trainingSchedule, validationSchedule
            );
            if bestValidation > localMisses {
                set bestValidation = localMisses;
                set bestSoFar = proposedUpdate;
            }

        }
        return (bestSoFar, bestValidation);
    }

    operation TrainingSample() : (Double[], Double) {
        let features = Features();
        let labels = Labels();
        let starting_points = [
            [0.060057, 3.00522,  2.03083,  0.63527,  1.03771, 1.27881, 4.10186,  5.34396],
            [0.586514, 3.371623, 0.860791, 2.92517,  1.14616, 2.99776, 2.26505,  5.62137],
            [1.69704,  1.13912,  2.3595,   4.037552, 1.63698, 1.27549, 0.328671, 0.302282],
            [5.21662,  6.04363,  0.224184, 1.53913,  1.64524, 4.79508, 1.49742,  1.545]
        ];


        return TrainHalfMoonModel(features, labels, starting_points);
    }
}
