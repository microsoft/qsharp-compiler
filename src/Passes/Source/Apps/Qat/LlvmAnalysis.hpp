#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

struct LlvmAnalyser
{
  /// Constructors
  /// @{
  explicit LlvmAnalyser(bool debug);

  // Default construction not allowed as this leads
  // to invalid configuration of the managers.
  LlvmAnalyser() = delete;

  // Copy construction prohibited due to restrictions
  // on the member variables.
  LlvmAnalyser(LlvmAnalyser const &) = delete;

  // Prefer move construction at all times.
  LlvmAnalyser(LlvmAnalyser &&) = default;

  // Default deconstruction.
  ~LlvmAnalyser() = default;
  /// @}

  /// Objects used to run a set of passes
  /// @{
  llvm::PassBuilder             pass_builder;
  llvm::LoopAnalysisManager     loop_analysis_manager;
  llvm::FunctionAnalysisManager function_analysis_manager;
  llvm::CGSCCAnalysisManager    gscc_analysis_manager;
  llvm::ModuleAnalysisManager   module_analysis_manager;
  /// @}
};

}  // namespace quantum
}  // namespace microsoft
