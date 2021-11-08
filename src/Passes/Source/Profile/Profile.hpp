#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "AllocationManager/IAllocationManager.hpp"
#include "QatTypes/QatTypes.hpp"
#include "Validator/Validator.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    class ProfileGenerator;

    /// Profile class that defines a set of rules which constitutes the profile definition. Each of the
    /// rules can be used to transform a generic QIR and/or validate that the QIR is compliant with said
    /// rule.
    class Profile
    {
      public:
        /// Allocation manager pointer type. Used to reference to concrete allocation manager
        /// implementations which defines the allocation logic of the profile.
        using AllocationManagerPtr = IAllocationManager::AllocationManagerPtr;

        /// Validator class used to check that an IR fulfils a given specification
        using ValidatorPtr = Validator::ValidatorPtr;

        // Constructors
        //

        explicit Profile(
            String const&        name,
            bool                 debug,
            llvm::TargetMachine* target_machine            = nullptr,
            AllocationManagerPtr qubit_allocation_manager  = BasicAllocationManager::createNew(),
            AllocationManagerPtr result_allocation_manager = BasicAllocationManager::createNew());

        // Default construction not allowed as this leads to invalid configuration of the allocation
        // managers.

        Profile()               = delete;
        Profile(Profile const&) = delete;
        Profile(Profile&&)      = default;
        Profile& operator=(Profile const&) = delete;
        Profile& operator=(Profile&&) = default;
        ~Profile()                    = default;

        // Profile methods
        //

        /// Applies the profile to a module.
        void apply(llvm::Module& module);

        /// Verifies that a module is a valid LLVM IR.
        bool verify(llvm::Module& module);

        /// Validates that a module complies with the specified QIR profile.
        bool validate(llvm::Module& module);

        AllocationManagerPtr getQubitAllocationManager();
        AllocationManagerPtr getResultAllocationManager();

        String const& name() const;

      protected:
        // Ensuring that ProfileGenerator has access to following protected functions.
        friend class ProfileGenerator;

        /// Sets the module pass manager used for the transformation of the IR.
        void setModulePassManager(llvm::ModulePassManager&& manager);

        /// Sets the validator
        void setValidator(ValidatorPtr&& validator);

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
        using PassInstrumentationCallbacksPtr = std::unique_ptr<llvm::PassInstrumentationCallbacks>;
        using StandardInstrumentationsPtr     = std::unique_ptr<llvm::StandardInstrumentations>;
        using PassBuilderPtr                  = std::unique_ptr<llvm::PassBuilder>;

        void registerEPCallbacks(bool verify_each_pass, bool debug);

        template <typename PassManager>
        bool tryParsePipelineText(llvm::PassBuilder& pass_builder, std::string const& pipeline_options)
        {
            if (pipeline_options.empty())
            {
                return false;
            }

            PassManager pass_manager;
            if (auto err = pass_builder.parsePassPipeline(pass_manager, pipeline_options))
            {
                llvm::errs() << "Could not parse -" << pipeline_options << " pipeline: " << toString(std::move(err))
                             << "\n";
                return false;
            }
            return true;
        }

        /// Name of the selected profile
        String name_{};

        // LLVM logic to run the passes
        //

        llvm::LoopAnalysisManager     loop_analysis_manager_;
        llvm::FunctionAnalysisManager function_analysis_manager_;
        llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
        llvm::ModuleAnalysisManager   module_analysis_manager_;

        llvm::Optional<llvm::PGOOptions> pgo_options_;
        PassInstrumentationCallbacksPtr  pass_instrumentation_callbacks_;
        StandardInstrumentationsPtr      standard_instrumentations_;
        llvm::PipelineTuningOptions      pipeline_tuning_options_;

        PassBuilderPtr pass_builder_;

        llvm::ModulePassManager module_pass_manager_{};

        // Allocation management
        //

        /// Interface pointer to the qubit allocation manager. Mode of operation depends on the concrete
        /// implementation of the manager which is swappable through the interface.
        AllocationManagerPtr qubit_allocation_manager_{};

        /// Interface pointer to the results allocation manager. Again here the manager behaviour is
        /// determined by its implementation details.
        AllocationManagerPtr result_allocation_manager_{};

        ///
        ValidatorPtr validator_{};

        std::string peephole_ep_pipeline_{""};
        std::string late_loop_optimizations_ep_pipeline_{""};
        std::string loop_optimizer_end_ep_pipeline_{""};
        std::string scalar_optimizer_late_ep_pipeline_{""};
        std::string cgscc_optimizer_late_ep_pipeline_{""};
        std::string vectorizer_start_ep_pipeline_{""};
        std::string pipeline_start_ep_pipeline_{""};
        std::string optimizer_last_ep_pipeline_{""};
    };

} // namespace quantum
} // namespace microsoft
