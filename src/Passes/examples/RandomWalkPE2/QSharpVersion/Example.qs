namespace Microsoft.Quantum.Qir.Emission {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Measurement;
    // Model parameters used in this program are expressed in units of π / 2, such that
    // φ = 1 indicates an eigenphase of π / 2. Since we work in terms of the
    // standard deviation and not the variance, all quantities other than the
    // prefactor 1 / 𝑒 have the same units (namely, radians rescaled by π),
    // while that prefactor has units of 1.
    //
    // In particular, since the eigenphase is bounded on the interval
    // [-π, π), using units of π / 2 gives us that φ ∈ [-2, 2). Note that this
    // depends on where we make the branch cut, as φ + 4 is always equally
    // valid under our likelihood function — this requires some caution in
    // setting the prior to ensure that the random walk doesn't approach the
    // branch cuts at φ = ±2.
    operation Prepare(target : Qubit) : Unit {
        H(target);
    }
    operation Iterate(c1 : Double, c2 : Double, target : Qubit, aux : Qubit) : Result {
        within {
            H(aux);
        } apply {
            RzPi(c1 / 2.0, aux);
            CRzPi(c2 / 2.0, aux, target);
        }
        return MResetZ(aux);
    }
    function ClassicalComputeInput1(mu : Double, sigma : Double) : Double {
        return 1.0 - (mu / sigma);
    }
    function ClassicalComputeInput2(mu : Double, sigma : Double) : Double {
        return 2.0 * (mu - sigma);
    }
    // function ClassicalCompute(mu : Double, sigma : Double) : (Double, Double) {
    //     let time = mu - PI() * sigma / 2.0;
    //     let theta = 1.0 / sigma;
    //     return (-theta * time, PI() * time);
    // }
    function ClassicalUpdateMu(mu : Double, sigma : Double, res : Result) : Double {
        return res == Zero ? mu - sigma * 0.6065 | mu + sigma * 0.6065;
    }
    function ClassicalUpdateSigma(sigma : Double) : Double {
        return sigma * 0.7951;
    }
    // function ClassicalUpdate(mu : Double, sigma : Double, res : Result) : (Double, Double) {
    //     return (res == Zero ? mu - sigma * 0.6065 | mu + sigma * 0.6065, sigma * 0.7951);
    // }
    @EntryPoint()
    operation EstimatePhaseByRandomWalk(nrIter : Int) : Double {
        mutable mu = 0.7951;
        mutable sigma = 0.6065;
        use target = Qubit();
        use aux = Qubit();
        Prepare(target);
        for _ in 1 .. nrIter {
            let c1 = ClassicalComputeInput1(mu, sigma);
            let c2 = ClassicalComputeInput2(mu, sigma);
            // let (c1, c2) = ClassicalCompute(mu, sigma);
            let datum = Iterate(c1, c2, target, aux);
            set mu = ClassicalUpdateMu(mu, sigma, datum);
            set sigma = ClassicalUpdateSigma(sigma);
            // set (mu, sigma) = ClassicalUpdate(mu, sigma, datum);
        }
        return mu;
    }
}