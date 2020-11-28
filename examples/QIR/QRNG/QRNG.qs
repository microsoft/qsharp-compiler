namespace Qrng {
    open Microsoft.Quantum.Intrinsic;

    operation RandomBit() : Result {
        using (q = Qubit())  {  // Allocate a qubit.
            let rslt = Measure([PauliX],[q]);
            return Measure([PauliZ],[q]);  // Measure the qubit value.
        }
    }

    operation RandomInt() : Int {
        mutable rslt = 0;
        for (i in 0..31) {
            let oneBit = RandomBit();
            if (oneBit == One) {
                set rslt = rslt + (1 <<< i);
            }
            
        }
        return rslt;
    }

    @EntryPoint()
    operation RandomInts() : Int[] {
        mutable rslts = new Int[32];
        for (i in 0..31) {
            set rslts w/= i <- RandomInt(); 
        }
        return rslts;
    }

}
