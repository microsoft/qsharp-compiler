// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#include "TestTools/ProfileTest.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

ProfileTest::ProfileTest(String const &data)
  : module_{llvm::parseIR(llvm::MemoryBufferRef(data, "ProfileTest"), error_, context_)}
{
  pass_builder_.registerModuleAnalyses(module_analysis_manager_);
  pass_builder_.registerCGSCCAnalyses(gscc_analysis_manager_);
  pass_builder_.registerFunctionAnalyses(function_analysis_manager_);
  pass_builder_.registerLoopAnalyses(loop_analysis_manager_);

  pass_builder_.crossRegisterProxies(loop_analysis_manager_, function_analysis_manager_,
                                     gscc_analysis_manager_, module_analysis_manager_);
}

llvm::PassBuilder &ProfileTest::passBuilder()
{
  return pass_builder_;
}
llvm::LoopAnalysisManager &ProfileTest::loopAnalysisManager()
{
  return loop_analysis_manager_;
}
llvm::FunctionAnalysisManager &ProfileTest::functionAnalysisManager()
{
  return function_analysis_manager_;
}
llvm::CGSCCAnalysisManager &ProfileTest::gsccAnalysisManager()
{
  return gscc_analysis_manager_;
}
llvm::ModuleAnalysisManager &ProfileTest::moduleAnalysisManager()
{
  return module_analysis_manager_;
}

ProfileTest::ModulePtr &ProfileTest::module()
{
  return module_;
}

}  // namespace quantum
}  // namespace microsoft
