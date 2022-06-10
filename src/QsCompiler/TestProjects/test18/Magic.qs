%azure.target ionq.simulator

namespace Test18 {
    operation PrintParity(num: Int): Int {
        let two = 2;
        let parity = num
%two
        ;
        return parity;
    }
}
