#include "QirContext.hpp"
#include "SimFactory.hpp"

#include <memory>

using namespace Microsoft::Quantum;

extern "C" void InitQIRSim()
{
    // initialize Quantum Simulator and QIR Runtime
    std::unique_ptr<IRuntimeDriver> sim = CreateFullstateSimulator();
    InitializeQirContext(sim.release(), true);
}
