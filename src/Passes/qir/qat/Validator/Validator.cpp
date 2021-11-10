// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "ValidationPass/ValidationPass.hpp"
#include "Validator/Validator.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    Validator::Validator(ValidationPassConfiguration const& cfg, bool debug, llvm::TargetMachine* target_machine)
      : loop_analysis_manager_{debug}
      , function_analysis_manager_{debug}
      , gscc_analysis_manager_{debug}
      , module_analysis_manager_{debug}
    {

        pass_builder_ = std::make_unique<llvm::PassBuilder>(target_machine);

        pass_builder_->registerModuleAnalyses(module_analysis_manager_);
        pass_builder_->registerCGSCCAnalyses(gscc_analysis_manager_);
        pass_builder_->registerFunctionAnalyses(function_analysis_manager_);
        pass_builder_->registerLoopAnalyses(loop_analysis_manager_);

        pass_builder_->crossRegisterProxies(
            loop_analysis_manager_, function_analysis_manager_, gscc_analysis_manager_, module_analysis_manager_);

        module_pass_manager_.addPass(ValidationPass(cfg));
    }

    bool Validator::validate(llvm::Module& module)
    {
        llvm::VerifierAnalysis verifier;
        auto                   result = verifier.run(module, module_analysis_manager_);

        if (result.IRBroken)
        {
            llvm::errs() << "; Fatal error: Invalid IR.\n";
            return false;
        }

        try
        {
            module_pass_manager_.run(module, module_analysis_manager_);
        }
        catch (std::exception const& e)
        {
            llvm::errs() << "; Fatal error: " << e.what() << "\n";
            return false;
        }
        return true;
    }

    void Validator::setModulePassManager(llvm::ModulePassManager&& manager)
    {
        module_pass_manager_ = std::move(manager);
    }

    llvm::PassBuilder& Validator::passBuilder()
    {
        return *pass_builder_;
    }
    llvm::LoopAnalysisManager& Validator::loopAnalysisManager()
    {
        return loop_analysis_manager_;
    }
    llvm::FunctionAnalysisManager& Validator::functionAnalysisManager()
    {
        return function_analysis_manager_;
    }
    llvm::CGSCCAnalysisManager& Validator::gsccAnalysisManager()
    {
        return gscc_analysis_manager_;
    }
    llvm::ModuleAnalysisManager& Validator::moduleAnalysisManager()
    {
        return module_analysis_manager_;
    }

} // namespace quantum
} // namespace microsoft
