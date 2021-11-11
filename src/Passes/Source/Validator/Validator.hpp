#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "AllocationManager/IAllocationManager.hpp"
#include "Logging/ILogger.hpp"
#include "Logging/LogCollection.hpp"
#include "ValidationPass/ValidationPassConfiguration.hpp"

#include "Llvm/Llvm.hpp"

#include <memory>

namespace microsoft
{
namespace quantum
{

    /// Validator class that defines a set of rules which constitutes the profile definition. Each of
    /// the rules can be used to transform a generic QIR and/or validate that the QIR is compliant with
    /// said rule.
    class Validator
    {
      public:
        using ValidatorPtr      = std::unique_ptr<Validator>;
        using LogColloectionPtr = std::shared_ptr<LogCollection>;

        // Constructors
        //

        explicit Validator(
            ValidationPassConfiguration const& cfg,
            bool                               debug,
            llvm::TargetMachine*               target_machine = nullptr);

        // Default construction not allowed to ensure that LLVM modules and passes are set up correctly.
        // Copy construction is prohibited due to restriction on classes held by Validator.

        Validator()                 = delete;
        Validator(Validator const&) = delete;
        Validator(Validator&&)      = default;
        Validator& operator=(Validator const&) = delete;
        Validator& operator=(Validator&&) = default;
        ~Validator()                      = default;

        // Validator methods
        //

        /// Validates that a module complies with the specified QIR profile. Returns true if the module is
        /// valid and false otherwise.
        bool validate(llvm::Module& module);

      protected:
        using PassBuilderPtr = std::unique_ptr<llvm::PassBuilder>;

        /// Returns a reference to the pass builder.
        llvm::PassBuilder& passBuilder();

        /// Returns a reference to the loop analysis manager.
        llvm::LoopAnalysisManager& loopAnalysisManager();

        /// Returns a reference to the function analysis manager.
        llvm::FunctionAnalysisManager& functionAnalysisManager();

        /// Returns a reference to the GSCC analysis manager.
        llvm::CGSCCAnalysisManager& gsccAnalysisManager();

        /// Returns a reference to the module analysis manager.
        llvm::ModuleAnalysisManager& moduleAnalysisManager();

      private:
        void saveReportToFileIfNeeded();

        // LLVM logic to run the passes
        //

        llvm::LoopAnalysisManager     loop_analysis_manager_;
        llvm::FunctionAnalysisManager function_analysis_manager_;
        llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
        llvm::ModuleAnalysisManager   module_analysis_manager_;

        PassBuilderPtr pass_builder_;

        llvm::ModulePassManager module_pass_manager_{};

        LogColloectionPtr logger_{nullptr}; ///< Logger to keep track of errors and warnings occurring.
        String            save_to_filename_{""};
    };

} // namespace quantum
} // namespace microsoft
