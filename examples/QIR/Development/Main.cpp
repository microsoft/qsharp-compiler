#include "QirContext.hpp"
#include "QirRuntime.hpp"
#include "SimFactory.hpp"

using namespace Microsoft::Quantum;
using namespace std;

extern "C" void Microsoft__Quantum__Qir__Development__RunExample(); // NOLINT
int main(int argc, char* argv[]){
    unique_ptr<IRuntimeDriver> sim = CreateFullstateSimulator();
    QirContextScope qirctx(sim.get(), false /*trackAllocatedObjects*/);
    Microsoft__Quantum__Qir__Development__RunExample();
    return 0;
}

