// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Profile/Profile.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

Profile::Profile(bool debug, AllocationManagerPtr qubit_allocation_manager,
                 AllocationManagerPtr result_allocation_manager, ValueTrackerPtr value_tracker)
  : loop_analysis_manager_{debug}
  , function_analysis_manager_{debug}
  , gscc_analysis_manager_{debug}
  , module_analysis_manager_{debug}
  , qubit_allocation_manager_{std::move(qubit_allocation_manager)}
  , result_allocation_manager_{std::move(result_allocation_manager)}
  , value_tracker_{value_tracker}
{
  // Creating a full pass builder and registering each of the
  // components to make them accessible to the developer.
  pass_builder_.registerModuleAnalyses(module_analysis_manager_);
  pass_builder_.registerCGSCCAnalyses(gscc_analysis_manager_);
  pass_builder_.registerFunctionAnalyses(function_analysis_manager_);
  pass_builder_.registerLoopAnalyses(loop_analysis_manager_);

  pass_builder_.crossRegisterProxies(loop_analysis_manager_, function_analysis_manager_,
                                     gscc_analysis_manager_, module_analysis_manager_);
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

bool Profile::validate(llvm::Module &)
{
  throw std::runtime_error("Validation is not supported yet.");
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

llvm::PassBuilder &Profile::passBuilder()
{
  return pass_builder_;
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
