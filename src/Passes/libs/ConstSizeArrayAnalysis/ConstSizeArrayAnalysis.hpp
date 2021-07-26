#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

namespace microsoft {
namespace quantum {

class ConstSizeArrayAnalysisAnalytics : public llvm::AnalysisInfoMixin<ConstSizeArrayAnalysisAnalytics>
{
public:
  using Result = llvm::StringMap<unsigned>; ///< Change the type of the collected date here

  /// Constructors and destructors
  /// @{
  ConstSizeArrayAnalysisAnalytics()                         = default;
  ConstSizeArrayAnalysisAnalytics(ConstSizeArrayAnalysisAnalytics const &)  = delete;
  ConstSizeArrayAnalysisAnalytics(ConstSizeArrayAnalysisAnalytics &&)       = default;
  ~ConstSizeArrayAnalysisAnalytics()                        = default;
  /// @}

  /// Operators
  /// @{
  ConstSizeArrayAnalysisAnalytics &operator=(ConstSizeArrayAnalysisAnalytics const &) = delete;
  ConstSizeArrayAnalysisAnalytics &operator=(ConstSizeArrayAnalysisAnalytics &&) = delete;
  /// @}

  /// Functions required by LLVM
  /// @{
  Result run(llvm::Function & function, llvm::FunctionAnalysisManager & /*unused*/);
  /// @}

private:
  static llvm::AnalysisKey Key;  // NOLINT
  friend struct llvm::AnalysisInfoMixin<ConstSizeArrayAnalysisAnalytics>;
};

class ConstSizeArrayAnalysisPrinter : public llvm::PassInfoMixin<ConstSizeArrayAnalysisPrinter>
{
public:
  /// Constructors and destructors
  /// @{
  explicit ConstSizeArrayAnalysisPrinter(llvm::raw_ostream& out_stream);  
  ConstSizeArrayAnalysisPrinter()                       = delete;
  ConstSizeArrayAnalysisPrinter(ConstSizeArrayAnalysisPrinter const &)  = delete;
  ConstSizeArrayAnalysisPrinter(ConstSizeArrayAnalysisPrinter &&)       = default;
  ~ConstSizeArrayAnalysisPrinter()                      = default;
  /// @}

  /// Operators
  /// @{
  ConstSizeArrayAnalysisPrinter &operator=(ConstSizeArrayAnalysisPrinter const &) = delete;
  ConstSizeArrayAnalysisPrinter &operator=(ConstSizeArrayAnalysisPrinter &&) = delete;
  /// @}

  /// Functions required by LLVM
  /// @{
  llvm::PreservedAnalyses run(llvm::Function & function, llvm::FunctionAnalysisManager & fam);
  static bool             isRequired();
  /// @}
private:
  llvm::raw_ostream& out_stream_;  
};

}  // namespace quantum
}  // namespace microsoft
