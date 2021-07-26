#include "QirContext.hpp"
#include "QirRuntime.hpp"
#include "SimFactory.hpp"

using namespace Microsoft::Quantum;
using namespace std;

extern "C" void Hello__HelloQ();

int main(int argc, char* argv[]){
    unique_ptr<IRuntimeDriver> sim = CreateFullstateSimulator();
    QirContextScope qirctx(sim.get(), true /*trackAllocatedObjects*/);
    Hello__HelloQ();
    return 0;
}
