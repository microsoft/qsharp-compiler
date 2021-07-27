#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

class ConstSizeArrayAnalysisAnalytics
  : public llvm::AnalysisInfoMixin<ConstSizeArrayAnalysisAnalytics>
{
public:
  using String  = std::string;
  using ArgList = std::unordered_set<std::string>;

  struct QubitArray
  {
    bool    is_possibly_static{false};  ///< Indicates whether the array is possibly static or not
    String  variable_name{};            ///< Name of the qubit array
    ArgList depends_on{};  ///< Function arguments that determines if it is constant or not
  };

  using Value                = llvm::Value;
  using DependencyGraph      = std::unordered_map<std::string, ArgList>;
  using ValueDependencyGraph = std::unordered_map<Value *, ArgList>;

  using Instruction    = llvm::Instruction;
  using Function       = llvm::Function;
  using QubitArrayList = std::vector<QubitArray>;
  using Result         = QubitArrayList;

  /// Constructors and destructors
  /// @{
  ConstSizeArrayAnalysisAnalytics()                                        = default;
  ConstSizeArrayAnalysisAnalytics(ConstSizeArrayAnalysisAnalytics const &) = delete;
  ConstSizeArrayAnalysisAnalytics(ConstSizeArrayAnalysisAnalytics &&)      = default;
  ~ConstSizeArrayAnalysisAnalytics()                                       = default;
  /// @}

  /// Operators
  /// @{
  ConstSizeArrayAnalysisAnalytics &operator=(ConstSizeArrayAnalysisAnalytics const &) = delete;
  ConstSizeArrayAnalysisAnalytics &operator=(ConstSizeArrayAnalysisAnalytics &&) = delete;
  /// @}

  /// Functions required by LLVM
  /// @{
  Result run(llvm::Function &function, llvm::FunctionAnalysisManager & /*unused*/);
  /// @}

  /// Function analysis
  /// @{
  void analyseFunction(llvm::Function &function);
  /// @}

  /// Instruction analysis
  /// @{
  bool operandsConstant(Instruction const &instruction) const;
  void markPossibleConstant(Instruction &instruction);
  void analyseCall(Instruction &instruction);
  /// @}

private:
  static llvm::AnalysisKey Key;  // NOLINT
  friend struct llvm::AnalysisInfoMixin<ConstSizeArrayAnalysisAnalytics>;

  /// Analysis details
  /// @{
  ValueDependencyGraph value_depending_on_args_{};
  /// @}

  /// Result
  /// @{
  QubitArrayList results_{};
  /// @}
};

class ConstSizeArrayAnalysisPrinter : public llvm::PassInfoMixin<ConstSizeArrayAnalysisPrinter>
{
public:
  /// Constructors and destructors
  /// @{
  explicit ConstSizeArrayAnalysisPrinter(llvm::raw_ostream &out_stream);
  ConstSizeArrayAnalysisPrinter()                                      = delete;
  ConstSizeArrayAnalysisPrinter(ConstSizeArrayAnalysisPrinter const &) = delete;
  ConstSizeArrayAnalysisPrinter(ConstSizeArrayAnalysisPrinter &&)      = default;
  ~ConstSizeArrayAnalysisPrinter()                                     = default;
  /// @}

  /// Operators
  /// @{
  ConstSizeArrayAnalysisPrinter &operator=(ConstSizeArrayAnalysisPrinter const &) = delete;
  ConstSizeArrayAnalysisPrinter &operator=(ConstSizeArrayAnalysisPrinter &&) = delete;
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
