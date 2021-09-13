// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Profile/Profile.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

Profile::Profile(bool debug)
  : loop_analysis_manager_{debug}
  , function_analysis_manager_{debug}
  , gscc_analysis_manager_{debug}
  , module_analysis_manager_{debug}
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
