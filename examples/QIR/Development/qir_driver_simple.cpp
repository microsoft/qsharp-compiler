#include "SimFactory.hpp"
#include "QirContext.hpp"
#include <iostream>

extern "C" void Microsoft__Quantum__Qir__Development__RunExample();

int main() {
    auto sim = Microsoft::Quantum::CreateFullstateSimulator();
    Microsoft::Quantum::QirExecutionContext::Init(sim.get());
    
    std::cout << "In driver; about to run exmample\n";
    Microsoft__Quantum__Qir__Development__RunExample();
    std::cout << "In driver; just ran example\n";
}