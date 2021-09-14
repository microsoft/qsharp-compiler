#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "AllocationManager/IAllocationManager.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    class Profile
    {
      public:
        using AllocationManagerPtr = IAllocationManager::AllocationManagerPtr;

        /// Constructors
        /// @{
        Profile(
            bool                 debug,
            AllocationManagerPtr qubit_allocation_manager   = BasicAllocationManager::createNew(),
            AllocationManagerPtr results_allocation_manager = BasicAllocationManager::createNew());

        // Default construction not allowed as this leads
        // to invalid configuration of the managers.
        Profile() = delete;

        // Copy construction prohibited due to restrictions
        // on the member variables.
        Profile(Profile const&) = delete;

        // Prefer move construction at all times.
        Profile(Profile&&) = default;

        // Default deconstruction.
        ~Profile() = default;
        /// @}

        void apply(llvm::Module& module);
        bool verify(llvm::Module& module);
        void setModulePassManager(llvm::ModulePassManager&& manager);

        AllocationManagerPtr getQubitAllocationManager();
        AllocationManagerPtr getResultAllocationManager();

        /// Acccess member functions
        /// @{
        llvm::PassBuilder&             passBuilder();
        llvm::LoopAnalysisManager&     loopAnalysisManager();
        llvm::FunctionAnalysisManager& functionAnalysisManager();
        llvm::CGSCCAnalysisManager&    gsccAnalysisManager();
        llvm::ModuleAnalysisManager&   moduleAnalysisManager();
        /// @}
      private:
        /// Objects used to run a set of passes
        /// @{
        llvm::PassBuilder             pass_builder_;
        llvm::LoopAnalysisManager     loop_analysis_manager_;
        llvm::FunctionAnalysisManager function_analysis_manager_;
        llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
        llvm::ModuleAnalysisManager   module_analysis_manager_;
        /// @}

        llvm::ModulePassManager module_pass_manager_{};

        AllocationManagerPtr qubit_allocation_manager_{};
        AllocationManagerPtr results_allocation_manager_{};
    };

} // namespace quantum
} // namespace microsoft
