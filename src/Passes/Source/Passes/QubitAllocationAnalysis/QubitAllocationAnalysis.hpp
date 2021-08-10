#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

class QubitAllocationAnalysisAnalytics
  : public llvm::AnalysisInfoMixin<QubitAllocationAnalysisAnalytics>
{
public:
  using String = std::string;

  struct Result
  {
    bool value{false};
  };

  /// Constructors and destructors
  /// @{
  QubitAllocationAnalysisAnalytics()                                         = default;
  QubitAllocationAnalysisAnalytics(QubitAllocationAnalysisAnalytics const &) = delete;
  QubitAllocationAnalysisAnalytics(QubitAllocationAnalysisAnalytics &&)      = default;
  ~QubitAllocationAnalysisAnalytics()                                        = default;
  /// @}

  /// Operators
  /// @{
  QubitAllocationAnalysisAnalytics &operator=(QubitAllocationAnalysisAnalytics const &) = delete;
  QubitAllocationAnalysisAnalytics &operator=(QubitAllocationAnalysisAnalytics &&) = delete;
  /// @}

  /// Functions required by LLVM
  /// @{
  Result run(llvm::Function &function, llvm::FunctionAnalysisManager & /*unused*/);
  /// @}

private:
  static llvm::AnalysisKey Key;  // NOLINT
  friend struct llvm::AnalysisInfoMixin<QubitAllocationAnalysisAnalytics>;
};

class QubitAllocationAnalysisPrinter : public llvm::PassInfoMixin<QubitAllocationAnalysisPrinter>
{
public:
  /// Constructors and destructors
  /// @{
  explicit QubitAllocationAnalysisPrinter(llvm::raw_ostream &out_stream);
  QubitAllocationAnalysisPrinter()                                       = delete;
  QubitAllocationAnalysisPrinter(QubitAllocationAnalysisPrinter const &) = delete;
  QubitAllocationAnalysisPrinter(QubitAllocationAnalysisPrinter &&)      = default;
  ~QubitAllocationAnalysisPrinter()                                      = default;
  /// @}

  /// Operators
  /// @{
  QubitAllocationAnalysisPrinter &operator=(QubitAllocationAnalysisPrinter const &) = delete;
  QubitAllocationAnalysisPrinter &operator=(QubitAllocationAnalysisPrinter &&) = delete;
  /// @}

  /// Functions required by LLVM
  /// @{
  llvm::PreservedAnalyses run(llvm::Function &function, llvm::FunctionAnalysisManager &fam);
  static bool             isRequired();
  /// @}
private:
  llvm::raw_ostream &out_stream_;
};

}  // namespace quantum
}  // namespace microsoft
