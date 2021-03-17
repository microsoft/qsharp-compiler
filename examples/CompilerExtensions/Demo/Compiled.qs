namespace Compiled {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;

    operation CRz(
        p1 : Double,
        q1 : Qubit, q2 : Qubit
    ) : Unit {
        body intrinsic;
    }

    operation Rz(
        p1 : Double,
        q1 : Qubit
    ) : Unit {
        body intrinsic;
    }

    operation M(q1 : Qubit) : Result {
        body intrinsic;
    }

    operation Preparation(q1 : Qubit) : Unit {
        H(q1);
    }

    operation Iteration(
        p1 : Double, p2 : Double,
        q1 : Qubit, q2 : Qubit
    ) : Result {

        Rz(p1, q2);
        CRz(p2, q2, q1);
        H(q2);
        return M(q2);
    }

    //@EntryPoint()
    operation PhaseEstimation(nrIter : Int) : Double {
            
        mutable mu = 0.7951;
        mutable sigma = 0.6065;

        use target = Qubit(); 
        Preparation(target);
    
        for _ in 1 .. nrIter {
    
            let time = mu - PI() * sigma / 2.0;
            let theta = 1.0 / sigma;

            use aux = Qubit();
            let p1 = -theta * time;
            let p2 = PI() * time;
            let datum = Iteration(p1, p2, target, aux);

            set mu = datum == Zero
                ? mu - sigma * 0.6065
                | mu + sigma * 0.6065;    
            set sigma *= 0.7951;
        }
    
        return mu;
    }
    
}
