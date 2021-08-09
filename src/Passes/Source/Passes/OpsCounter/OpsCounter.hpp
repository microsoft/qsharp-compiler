#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    class OpsCounterAnalytics : public llvm::AnalysisInfoMixin<OpsCounterAnalytics>
    {
      public:
        using Result = llvm::StringMap<unsigned>;

        /// Constructors and destructors
        /// @{
        OpsCounterAnalytics()                           = default;
        OpsCounterAnalytics(OpsCounterAnalytics const&) = delete;
        OpsCounterAnalytics(OpsCounterAnalytics&&)      = default;
        ~OpsCounterAnalytics()                          = default;
        /// @}

        /// Operators
        /// @{
        OpsCounterAnalytics& operator=(OpsCounterAnalytics const&) = delete;
        OpsCounterAnalytics& operator=(OpsCounterAnalytics&&) = delete;
        /// @}

        /// Functions required by LLVM
        /// @{
        Result run(llvm::Function& function, llvm::FunctionAnalysisManager& /*unused*/);
        /// @}

      private:
        static llvm::AnalysisKey Key; // NOLINT
        friend struct llvm::AnalysisInfoMixin<OpsCounterAnalytics>;
    };

    class OpsCounterPrinter : public llvm::PassInfoMixin<OpsCounterPrinter>
    {
      public:
        /// Constructors and destructors
        /// @{
        explicit OpsCounterPrinter(llvm::raw_ostream& out_stream);
        OpsCounterPrinter()                         = delete;
        OpsCounterPrinter(OpsCounterPrinter const&) = delete;
        OpsCounterPrinter(OpsCounterPrinter&&)      = default;
        ~OpsCounterPrinter()                        = default;
        /// @}

        /// Operators
        /// @{
        OpsCounterPrinter& operator=(OpsCounterPrinter const&) = delete;
        OpsCounterPrinter& operator=(OpsCounterPrinter&&) = delete;
        /// @}

        /// Functions required by LLVM
        /// @{
        llvm::PreservedAnalyses run(llvm::Function& function, llvm::FunctionAnalysisManager& fam);
        static bool             isRequired();
        /// @}
      private:
        llvm::raw_ostream& out_stream_;
    };

} // namespace quantum
} // namespace microsoft
