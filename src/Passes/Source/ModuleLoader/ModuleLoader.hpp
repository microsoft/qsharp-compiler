#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "QatTypes/QatTypes.hpp"
#include "RemoveDisallowedAttributesPass/RemoveDisallowedAttributesPass.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    class ModuleLoader
    {
      public:
        using Module       = llvm::Module;
        using Linker       = llvm::Linker;
        using SMDiagnostic = llvm::SMDiagnostic;

        explicit ModuleLoader(Module* final_module)
          : final_module_{final_module}
          , linker_{*final_module}

        {
        }

        bool addModule(std::unique_ptr<Module>&& module, String const& filename = "unknown")
        {
            if (llvm::verifyModule(*module, &llvm::errs()))
            {
                llvm::errs() << filename << ": "
                             << "input module is broken!\n";
                return false;
            }

            return !linker_.linkInModule(std::move(module), Linker::Flags::None);
        }

        bool addIrFile(String const& filename)
        {

            // Loading module
            SMDiagnostic            err;
            std::unique_ptr<Module> module = llvm::parseIRFile(filename, err, final_module_->getContext());
            if (!module)
            {
                llvm::errs() << "Failed to load " << filename << "\n";
                return false;
            }

            // Transforming module
            SingleModuleTransformation transformation;
            if (!transformation.apply(module.get()))
            {
                llvm::errs() << "Failed to transform " << filename << "\n";
                return false;
            }

            // Linking
            return addModule(std::move(module), filename);
        }

      private:
        Module* final_module_;
        Linker  linker_;

        // Single Module Transformation
        //

        class SingleModuleTransformation
        {
          public:
            using PassBuilder             = llvm::PassBuilder;
            using OptimizationLevel       = PassBuilder::OptimizationLevel;
            using FunctionAnalysisManager = llvm::FunctionAnalysisManager;

            explicit SingleModuleTransformation(
                OptimizationLevel const& optimisation_level = OptimizationLevel::O0,
                bool                     debug              = false)
              : loop_analysis_manager_{debug}
              , function_analysis_manager_{debug}
              , gscc_analysis_manager_{debug}
              , module_analysis_manager_{debug}
              , optimisation_level_{optimisation_level}
              , debug_{debug}
            {

                pass_builder_.registerModuleAnalyses(module_analysis_manager_);
                pass_builder_.registerCGSCCAnalyses(gscc_analysis_manager_);
                pass_builder_.registerFunctionAnalyses(function_analysis_manager_);
                pass_builder_.registerLoopAnalyses(loop_analysis_manager_);

                pass_builder_.crossRegisterProxies(
                    loop_analysis_manager_, function_analysis_manager_, gscc_analysis_manager_,
                    module_analysis_manager_);

                module_pass_manager_.addPass(RemoveDisallowedAttributesPass());
            }

            bool apply(llvm::Module* module)
            {
                module_pass_manager_.run(*module, module_analysis_manager_);

                if (llvm::verifyModule(*module, &llvm::errs()))
                {
                    return false;
                }

                return true;
            }

            bool isDebugMode() const
            {
                return debug_;
            }

          private:
            llvm::PassBuilder             pass_builder_;
            llvm::LoopAnalysisManager     loop_analysis_manager_;
            llvm::FunctionAnalysisManager function_analysis_manager_;
            llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
            llvm::ModuleAnalysisManager   module_analysis_manager_;

            llvm::ModulePassManager module_pass_manager_{};
            OptimizationLevel       optimisation_level_{};
            bool                    debug_{false};
        };
    };

} // namespace quantum
} // namespace microsoft
