namespace Microsoft.Quantum.Core {

    @Attribute()
    newtype Attribute = Unit;

    @Attribute()
    newtype EntryPoint = Unit;

    @Attribute()
    newtype Inline = Unit;
}

namespace Microsoft.Quantum.Targeting {

    @Attribute()
    newtype TargetInstruction = String;
}

namespace Microsoft.Quantum.Qir.Emission {

    @EntryPoint()
    operation EstimatePhaseByRandomWalk(nrIter : Int) : Double {
            
        mutable mu = 0.7951;
        mutable sigma = 0.6065;

        use target = Qubit(); 
    
        for _ in 1 .. nrIter {
        
            set mu -= sigma * 0.6065;
            set sigma *= 0.7951;
        }
        return mu;
    }
}


