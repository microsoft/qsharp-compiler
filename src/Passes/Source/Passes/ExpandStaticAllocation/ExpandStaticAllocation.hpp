#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/QirAllocationAnalysis/QirAllocationAnalysis.hpp"

#include "Llvm/Llvm.hpp"

#include <unordered_map>

namespace microsoft
{
namespace quantum
{

    /// This class copies functions which does static qubit and/or result allocation. This is done
    /// to ensure that qubits/result registers are not reused but instead assigned unique ids.
    class ExpandStaticAllocationPass : public llvm::PassInfoMixin<ExpandStaticAllocationPass>
    {
      public:
        using QubitAllocationResult = QirAllocationAnalysis::Result;
        using ConstantArguments     = std::unordered_map<std::string, llvm::ConstantInt*>;

        /// Constructors and destructors
        /// @{
        ExpandStaticAllocationPass()                                  = default;
        ExpandStaticAllocationPass(ExpandStaticAllocationPass const&) = default;
        ExpandStaticAllocationPass(ExpandStaticAllocationPass&&)      = default;
        ~ExpandStaticAllocationPass()                                 = default;
        /// @}

        /// Operators
        /// @{
        ExpandStaticAllocationPass& operator=(ExpandStaticAllocationPass const&) = default;
        ExpandStaticAllocationPass& operator=(ExpandStaticAllocationPass&&) = default;
        /// @}

        /// Functions required by LLVM
        /// @{
        llvm::PreservedAnalyses run(llvm::Function& function, llvm::FunctionAnalysisManager& fam);
        static bool             isRequired();
        /// @}

        /// @{
        llvm::Function* expandFunctionCall(llvm::Function& callee, ConstantArguments const& const_args);
        /// @}
    };

} // namespace quantum
} // namespace microsoft
