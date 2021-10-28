// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Profile/Profile.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

Profile::Profile(String const &name, bool debug, llvm::TargetMachine *target_machine,
                 AllocationManagerPtr qubit_allocation_manager,
                 AllocationManagerPtr result_allocation_manager, ValueTrackerPtr value_tracker)
  : name_{name}
  , loop_analysis_manager_{debug}
  , function_analysis_manager_{debug}
  , gscc_analysis_manager_{debug}
  , module_analysis_manager_{debug}
  , pgo_options_{}
  , pass_instrumentation_callbacks_{std::make_unique<llvm::PassInstrumentationCallbacks>()}
  , standard_instrumentations_{std::make_unique<llvm::StandardInstrumentations>()}
  , pipeline_tuning_options_{}
  , qubit_allocation_manager_{std::move(qubit_allocation_manager)}
  , result_allocation_manager_{std::move(result_allocation_manager)}
  , validator_{std::make_unique<Validator>(ValidationPassConfiguration(), debug)}
  , value_tracker_{std::move(value_tracker)}
{
  bool verify_each_pass = false;
  standard_instrumentations_->registerCallbacks(*pass_instrumentation_callbacks_);

  // TODO: Parameterize
  // pipeline_tuning_options_.LoopUnrolling = !DisableLoopUnrolling;
  // pipeline_tuning_options_.Coroutines = Coroutines;

  pass_builder_ =
      std::make_unique<llvm::PassBuilder>(target_machine, pipeline_tuning_options_, pgo_options_,
                                          pass_instrumentation_callbacks_.get());

  registerEPCallbacks(verify_each_pass, debug);

  // Creating a full pass builder and registering each of the
  // components to make them accessible to the developer.
  pass_builder_->registerModuleAnalyses(module_analysis_manager_);
  pass_builder_->registerCGSCCAnalyses(gscc_analysis_manager_);
  pass_builder_->registerFunctionAnalyses(function_analysis_manager_);
  pass_builder_->registerLoopAnalyses(loop_analysis_manager_);

  pass_builder_->crossRegisterProxies(loop_analysis_manager_, function_analysis_manager_,
                                      gscc_analysis_manager_, module_analysis_manager_);
}

void Profile::registerEPCallbacks(bool verify_each_pass, bool debug)
{

  if (tryParsePipelineText<llvm::FunctionPassManager>(*pass_builder_, PeepholeEPPipeline))
    pass_builder_->registerPeepholeEPCallback(
        [this, verify_each_pass, debug](llvm::FunctionPassManager &pass_manager,
                                        llvm::PassBuilder::OptimizationLevel) {
          llvm::ExitOnError error_safeguard("Unable to parse PeepholeEP pipeline: ");
          error_safeguard(pass_builder_->parsePassPipeline(pass_manager, PeepholeEPPipeline,
                                                           verify_each_pass, debug));
        });

  if (tryParsePipelineText<llvm::LoopPassManager>(*pass_builder_, LateLoopOptimizationsEPPipeline))
  {
    pass_builder_->registerLateLoopOptimizationsEPCallback(
        [this, verify_each_pass, debug](llvm::LoopPassManager &pass_manager,
                                        llvm::PassBuilder::OptimizationLevel) {
          llvm::ExitOnError error_safeguard("Unable to parse LateLoopOptimizationsEP pipeline: ");
          error_safeguard(pass_builder_->parsePassPipeline(
              pass_manager, LateLoopOptimizationsEPPipeline, verify_each_pass, debug));
        });
  }

  if (tryParsePipelineText<llvm::LoopPassManager>(*pass_builder_, LoopOptimizerEndEPPipeline))
  {
    pass_builder_->registerLoopOptimizerEndEPCallback(
        [this, verify_each_pass, debug](llvm::LoopPassManager &pass_manager,
                                        llvm::PassBuilder::OptimizationLevel) {
          llvm::ExitOnError error_safeguard("Unable to parse LoopOptimizerEndEP pipeline: ");
          error_safeguard(pass_builder_->parsePassPipeline(pass_manager, LoopOptimizerEndEPPipeline,
                                                           verify_each_pass, debug));
        });
  }

  if (tryParsePipelineText<llvm::FunctionPassManager>(*pass_builder_,
                                                      ScalarOptimizerLateEPPipeline))
  {
    pass_builder_->registerScalarOptimizerLateEPCallback(
        [this, verify_each_pass, debug](llvm::FunctionPassManager &pass_manager,
                                        llvm::PassBuilder::OptimizationLevel) {
          llvm::ExitOnError error_safeguard("Unable to parse ScalarOptimizerLateEP pipeline: ");
          error_safeguard(pass_builder_->parsePassPipeline(
              pass_manager, ScalarOptimizerLateEPPipeline, verify_each_pass, debug));
        });
  }

  if (tryParsePipelineText<llvm::CGSCCPassManager>(*pass_builder_, CGSCCOptimizerLateEPPipeline))
  {
    pass_builder_->registerCGSCCOptimizerLateEPCallback(
        [this, verify_each_pass, debug](llvm::CGSCCPassManager &pass_manager,
                                        llvm::PassBuilder::OptimizationLevel) {
          llvm::ExitOnError error_safeguard("Unable to parse CGSCCOptimizerLateEP pipeline: ");
          error_safeguard(pass_builder_->parsePassPipeline(
              pass_manager, CGSCCOptimizerLateEPPipeline, verify_each_pass, debug));
        });
  }

  if (tryParsePipelineText<llvm::FunctionPassManager>(*pass_builder_, VectorizerStartEPPipeline))
  {
    pass_builder_->registerVectorizerStartEPCallback(
        [this, verify_each_pass, debug](llvm::FunctionPassManager &pass_manager,
                                        llvm::PassBuilder::OptimizationLevel) {
          llvm::ExitOnError error_safeguard("Unable to parse VectorizerStartEP pipeline: ");
          error_safeguard(pass_builder_->parsePassPipeline(pass_manager, VectorizerStartEPPipeline,
                                                           verify_each_pass, debug));
        });
  }

  if (tryParsePipelineText<llvm::ModulePassManager>(*pass_builder_, PipelineStartEPPipeline))
  {
    pass_builder_->registerPipelineStartEPCallback(
        [this, verify_each_pass, debug](llvm::ModulePassManager &pass_manager) {
          llvm::ExitOnError error_safeguard("Unable to parse PipelineStartEP pipeline: ");
          error_safeguard(pass_builder_->parsePassPipeline(pass_manager, PipelineStartEPPipeline,
                                                           verify_each_pass, debug));
        });
  }

  if (tryParsePipelineText<llvm::FunctionPassManager>(*pass_builder_, OptimizerLastEPPipeline))
  {
    pass_builder_->registerOptimizerLastEPCallback(
        [this, verify_each_pass, debug](llvm::ModulePassManager &pass_manager,
                                        llvm::PassBuilder::OptimizationLevel) {
          llvm::ExitOnError error_safeguard("Unable to parse OptimizerLastEP pipeline: ");
          error_safeguard(pass_builder_->parsePassPipeline(pass_manager, OptimizerLastEPPipeline,
                                                           verify_each_pass, debug));
        });
  }
}

void Profile::apply(llvm::Module &module)
{
  module_pass_manager_.run(module, module_analysis_manager_);
}

bool Profile::verify(llvm::Module &module)
{
  llvm::VerifierAnalysis verifier;
  auto                   result = verifier.run(module, module_analysis_manager_);
  return !result.IRBroken;
}

bool Profile::validate(llvm::Module &module)
{
  return validator_->validate(module);
}

Profile::String Profile::name() const
{
  return name_;
}

Profile::AllocationManagerPtr Profile::getQubitAllocationManager()
{
  return qubit_allocation_manager_;
}

Profile::AllocationManagerPtr Profile::getResultAllocationManager()
{
  return result_allocation_manager_;
}

void Profile::setModulePassManager(llvm::ModulePassManager &&manager)
{
  module_pass_manager_ = std::move(manager);
}

void Profile::setValidator(ValidatorPtr &&validator)
{
  validator_ = std::move(validator);
}

llvm::PassBuilder &Profile::passBuilder()
{
  return *pass_builder_;
}
llvm::LoopAnalysisManager &Profile::loopAnalysisManager()
{
  return loop_analysis_manager_;
}
llvm::FunctionAnalysisManager &Profile::functionAnalysisManager()
{
  return function_analysis_manager_;
}
llvm::CGSCCAnalysisManager &Profile::gsccAnalysisManager()
{
  return gscc_analysis_manager_;
}
llvm::ModuleAnalysisManager &Profile::moduleAnalysisManager()
{
  return module_analysis_manager_;
}

}  // namespace quantum
}  // namespace microsoft
