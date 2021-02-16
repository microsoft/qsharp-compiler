namespace Microsoft.Quantum.Samples.Chemistry.SimpleVQE {
    open Microsoft.Quantum.Simulation; // only used for type declarations

    @EntryPoint()
    operation GetEnergyHydrogenVQE() : String {
        let evolutionSet = JordanWignerClusterOperatorEvolutionSet();
        let generatorSystem = JordanWignerClusterOperatorGeneratorSystem();
        let (nTerms, generatorSystemFunction) = generatorSystem!;
        let generatorIndex = generatorSystemFunction(0);
        let eSet = evolutionSet!(generatorIndex); // THIS ONE FAILS
        return "Success!";
    }

    function JordanWignerClusterOperatorEvolutionSet () : EvolutionSet {        
        return EvolutionSet(_JordanWignerClusterOperatorFunction);
    }

    function _JordanWignerClusterOperatorFunction (generatorIndex : GeneratorIndex) : EvolutionUnitary {
        return EvolutionUnitary(Dummy);
    }

    operation Dummy(d : Double, qs : Qubit[]) : Unit is Adj + Ctl{}

    function JordanWignerClusterOperatorGeneratorSystem () : GeneratorSystem {
        return GeneratorSystem(1, _JordanWignerClusterOperatorGeneratorSystemImpl);
    }

    function _JordanWignerClusterOperatorGeneratorSystemImpl(idx: Int) : GeneratorIndex {
        return GeneratorIndex(([1,2], [1.,2.]), [1,2]);
    }
}
