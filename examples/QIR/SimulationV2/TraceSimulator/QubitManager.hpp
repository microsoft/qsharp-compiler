//#include <unordered_map>

#include "CoreTypes.hpp"

namespace Microsoft
{
namespace Quantum
{
    class QubitManager
    {
        // Keep track of unique qubit ids via simple counter,
        // mapped to a "small" register of active qubits.
        uint64_t nextQubitId = 0;
        //std::unordered_map<uint64_t, short> qubitMap;  // qubit id -> register index
        //std::vector<uint64_t> computeRegister;         // register index -> qubit id

        // Get the internal ID associated to a qubit object.
        static uint64_t GetQubitId(Qubit qubit)
        {
            return reinterpret_cast<uint64_t>(qubit);
        }

      public:
        // Get the qubit index in the compute register.
        //short GetQubitIdx(Qubit qubit)
        //{
        //    return this->qubitMap[GetQubitId(qubit)];
        //}

        // Get a human-readable name for the qubit.
        std::string GetQubitName(Qubit qubit)
        {
            return std::to_string(GetQubitId(qubit));
        }

        Qubit AllocateQubit()
        {
        //    this->qubitMap[this->nextQubitId] = this->computeRegister.size();
        //    this->computeRegister.push_back(this->nextQubitId);
            return reinterpret_cast<Qubit>(this->nextQubitId++);
        }

        void ReleaseQubit(Qubit qubit)
        {
        //    this->computeRegister.erase(this->computeRegister.begin()+GetQubitIdx(qubit));
        //    for (int i = GetQubitIdx(qubit); i < this->computeRegister.size(); i++)
        //        this->qubitMap[this->computeRegister[i]] -= 1;
        //    this->qubitMap.erase(GetQubitId(qubit));
        }
    };

} // namespace Quantum
} // namespace Microsoft
