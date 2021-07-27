#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    class QubitAllocationAnalysisAnalytics : public llvm::AnalysisInfoMixin<QubitAllocationAnalysisAnalytics>
    {
      public:
        using String  = std::string;
        using ArgList = std::unordered_set<std::string>;

        struct QubitArray
        {
            bool is_possibly_static{false};           ///< Indicates whether the array is
                                                      /// possibly static or not
                                                      ///
            String  variable_name{};                  ///< Name of the qubit array
            ArgList depends_on{};                     ///< Function arguments that
                                                      /// determines if it is constant or not
                                                      ///
            uint64_t size{static_cast<uint64_t>(-1)}; ///< Size of the array if it can be deduced.
        };

        using Value                = llvm::Value;
        using DependencyGraph      = std::unordered_map<std::string, ArgList>;
        using ValueDependencyGraph = std::unordered_map<Value*, ArgList>;

        using Instruction = llvm::Instruction;
        using Function    = llvm::Function;
        using Result      = std::vector<QubitArray>;

        /// Constructors and destructors
        /// @{
        QubitAllocationAnalysisAnalytics()                                        = default;
        QubitAllocationAnalysisAnalytics(QubitAllocationAnalysisAnalytics const&) = delete;
        QubitAllocationAnalysisAnalytics(QubitAllocationAnalysisAnalytics&&)      = default;
        ~QubitAllocationAnalysisAnalytics()                                       = default;
        /// @}

        /// Operators
        /// @{
        QubitAllocationAnalysisAnalytics& operator=(QubitAllocationAnalysisAnalytics const&) = delete;
        QubitAllocationAnalysisAnalytics& operator=(QubitAllocationAnalysisAnalytics&&) = delete;
        /// @}

        /// Functions required by LLVM
        /// @{
        Result run(llvm::Function& function, llvm::FunctionAnalysisManager& /*unused*/);
        /// @}

        /// Function analysis
        /// @{
        void analyseFunction(llvm::Function& function);
        /// @}

        /// Instruction analysis
        /// @{
        bool operandsConstant(Instruction const& instruction) const;
        void markPossibleConstant(Instruction& instruction);
        void analyseCall(Instruction& instruction);
        /// @}

      private:
        static llvm::AnalysisKey Key; // NOLINT
        friend struct llvm::AnalysisInfoMixin<QubitAllocationAnalysisAnalytics>;

        /// Analysis details
        /// @{
        ValueDependencyGraph constantness_dependencies_{};
        /// @}

        /// Result
        /// @{
        Result results_{};
        /// @}
    };

    class QubitAllocationAnalysisPrinter : public llvm::PassInfoMixin<QubitAllocationAnalysisPrinter>
    {
      public:
        /// Constructors and destructors
        /// @{
        explicit QubitAllocationAnalysisPrinter(llvm::raw_ostream& out_stream);
        QubitAllocationAnalysisPrinter()                                      = delete;
        QubitAllocationAnalysisPrinter(QubitAllocationAnalysisPrinter const&) = delete;
        QubitAllocationAnalysisPrinter(QubitAllocationAnalysisPrinter&&)      = default;
        ~QubitAllocationAnalysisPrinter()                                     = default;
        /// @}

        /// Operators
        /// @{
        QubitAllocationAnalysisPrinter& operator=(QubitAllocationAnalysisPrinter const&) = delete;
        QubitAllocationAnalysisPrinter& operator=(QubitAllocationAnalysisPrinter&&) = delete;
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
