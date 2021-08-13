// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Apps/Qat/LlvmAnalysis.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

LlvmAnalyser::LlvmAnalyser(bool debug)
  : loop_analysis_manager{debug}
  , function_analysis_manager{debug}
  , gscc_analysis_manager{debug}
  , module_analysis_manager{debug}
{
  pass_builder.registerModuleAnalyses(module_analysis_manager);
  pass_builder.registerCGSCCAnalyses(gscc_analysis_manager);
  pass_builder.registerFunctionAnalyses(function_analysis_manager);
  pass_builder.registerLoopAnalyses(loop_analysis_manager);

  pass_builder.crossRegisterProxies(loop_analysis_manager, function_analysis_manager,
                                    gscc_analysis_manager, module_analysis_manager);
}

}  // namespace quantum
}  // namespace microsoft
