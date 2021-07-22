#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

class COpsCounterAnalytics : public llvm::AnalysisInfoMixin<COpsCounterAnalytics>
{
  public:
    using Result = llvm::StringMap<unsigned>;

    /// Constructors and destructors
    /// @{
    COpsCounterAnalytics()                            = default;
    COpsCounterAnalytics(COpsCounterAnalytics const&) = delete;
    COpsCounterAnalytics(COpsCounterAnalytics&&)      = default;
    ~COpsCounterAnalytics()                           = default;
    /// @}

    /// Operators
    /// @{
    COpsCounterAnalytics& operator=(COpsCounterAnalytics const&) = delete;
    COpsCounterAnalytics& operator=(COpsCounterAnalytics&&) = delete;
    /// @}

    /// Functions required by LLVM
    /// @{
    Result run(llvm::Function& function, llvm::FunctionAnalysisManager& /*unused*/);
    /// @}
  private:
    static llvm::AnalysisKey Key;
    friend struct llvm::AnalysisInfoMixin<COpsCounterAnalytics>;
};

class COpsCounterPrinter : public llvm::PassInfoMixin<COpsCounterPrinter>
{
  public:
    /// Constructors and destructors
    /// @{
    explicit COpsCounterPrinter(llvm::raw_ostream& out_stream);
    COpsCounterPrinter()                          = delete;
    COpsCounterPrinter(COpsCounterPrinter const&) = delete;
    COpsCounterPrinter(COpsCounterPrinter&&)      = default;
    ~COpsCounterPrinter()                         = default;
    /// @}

    /// Operators
    /// @{
    COpsCounterPrinter& operator=(COpsCounterPrinter const&) = delete;
    COpsCounterPrinter& operator=(COpsCounterPrinter&&) = delete;
    /// @}

    /// Functions required by LLVM
    /// @{
    llvm::PreservedAnalyses run(llvm::Function& function, llvm::FunctionAnalysisManager& fam);
    static bool             isRequired();
    /// @}
  private:
    llvm::raw_ostream& out_stream_;
};
