namespace Microsoft.Quantum.Qir.Emission {

    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;

    operation Prepare(target : Qubit) : Unit {
        H(target);
    }

    operation Iterate(time : Double, theta : Double, target : Qubit) : Result {

        use aux = Qubit();
        within {
            H(aux);
        } apply {
            Rz(-theta * time, aux);
            Controlled Rz([aux], (PI() * time, target));
        }
        return M(aux);
    }

    @EntryPoint()
    operation EstimatePhaseByRandomWalk(nrIter : Int) : Double {
            
        mutable mu = 0.7951;
        mutable sigma = 0.6065;

        use target = Qubit(); 
        Prepare(target);
    
        for _ in 1 .. nrIter {
    
            let time = mu - PI() * sigma / 2.0;
            let theta = 1.0 / sigma;

            let datum = Iterate(time, theta, target);
    
            set mu = datum == Zero
                ? mu - sigma * 0.6065
                | mu + sigma * 0.6065;    
            set sigma *= 0.7951;
        }
        return mu;
    }
}


