#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    class QirAllocationAnalysisAnalytics : public llvm::AnalysisInfoMixin<QirAllocationAnalysisAnalytics>
    {
      public:
        using String = std::string;

        struct Result
        {
            bool value{false};
        };

        /// Constructors and destructors
        /// @{
        QirAllocationAnalysisAnalytics()                                      = default;
        QirAllocationAnalysisAnalytics(QirAllocationAnalysisAnalytics const&) = delete;
        QirAllocationAnalysisAnalytics(QirAllocationAnalysisAnalytics&&)      = default;
        ~QirAllocationAnalysisAnalytics()                                     = default;
        /// @}

        /// Operators
        /// @{
        QirAllocationAnalysisAnalytics& operator=(QirAllocationAnalysisAnalytics const&) = delete;
        QirAllocationAnalysisAnalytics& operator=(QirAllocationAnalysisAnalytics&&) = delete;
        /// @}

        /// Functions required by LLVM
        /// @{
        Result run(llvm::Function& function, llvm::FunctionAnalysisManager& /*unused*/);
        /// @}

      private:
        static llvm::AnalysisKey Key; // NOLINT
        friend struct llvm::AnalysisInfoMixin<QirAllocationAnalysisAnalytics>;
    };

    class QirAllocationAnalysisPrinter : public llvm::PassInfoMixin<QirAllocationAnalysisPrinter>
    {
      public:
        /// Constructors and destructors
        /// @{
        explicit QirAllocationAnalysisPrinter(llvm::raw_ostream& out_stream);
        QirAllocationAnalysisPrinter()                                    = delete;
        QirAllocationAnalysisPrinter(QirAllocationAnalysisPrinter const&) = delete;
        QirAllocationAnalysisPrinter(QirAllocationAnalysisPrinter&&)      = default;
        ~QirAllocationAnalysisPrinter()                                   = default;
        /// @}

        /// Operators
        /// @{
        QirAllocationAnalysisPrinter& operator=(QirAllocationAnalysisPrinter const&) = delete;
        QirAllocationAnalysisPrinter& operator=(QirAllocationAnalysisPrinter&&) = delete;
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
