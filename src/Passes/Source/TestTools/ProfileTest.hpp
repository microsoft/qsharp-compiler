#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

class ProfileTest
{
public:
  using String       = std::string;
  using LLVMContext  = llvm::LLVMContext;
  using SMDiagnostic = llvm::SMDiagnostic;
  using Module       = llvm::Module;
  using ModulePtr    = std::unique_ptr<Module>;

  ProfileTest(String const &data);

  /// Acccess member functions
  /// @{
  llvm::PassBuilder &            passBuilder();
  llvm::LoopAnalysisManager &    loopAnalysisManager();
  llvm::FunctionAnalysisManager &functionAnalysisManager();
  llvm::CGSCCAnalysisManager &   gsccAnalysisManager();
  llvm::ModuleAnalysisManager &  moduleAnalysisManager();
  ModulePtr &                    module();
  /// @}
private:
  /// @{
  SMDiagnostic error_;
  LLVMContext  context_;
  ModulePtr    module_;
  /// @}

  /// Objects used to run a set of passes
  /// @{
  llvm::PassBuilder             pass_builder_;
  llvm::LoopAnalysisManager     loop_analysis_manager_;
  llvm::FunctionAnalysisManager function_analysis_manager_;
  llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
  llvm::ModuleAnalysisManager   module_analysis_manager_;
  /// @}
};

}  // namespace quantum
}  // namespace microsoft
