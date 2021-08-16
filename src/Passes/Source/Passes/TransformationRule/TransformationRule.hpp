#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/RuleSet.hpp"

#include "Llvm/Llvm.hpp"

#include <vector>

namespace microsoft
{
namespace quantum
{

    /// This class applies a set of transformation rules to the IR to transform it into a new IR. The
    /// rules are added using the RuleSet class which allows the developer to create one or more rules
    /// on how to transform the IR.
    class TransformationRulePass : public llvm::PassInfoMixin<TransformationRulePass>
    {
      public:
        using Replacements         = ReplacementRule::Replacements;
        using Instruction          = llvm::Instruction;
        using Rules                = std::vector<ReplacementRule>;
        using Value                = llvm::Value;
        using Builder              = ReplacementRule::Builder;
        using AllocationManagerPtr = AllocationManager::AllocationManagerPtr;

        /// Constructors and destructors
        /// @{
        explicit TransformationRulePass(RuleSet&& rule_set);
        TransformationRulePass(TransformationRulePass const&) = delete;
        TransformationRulePass(TransformationRulePass&&)      = default;
        ~TransformationRulePass()                             = default;
        /// @}

        /// Operators
        /// @{
        TransformationRulePass& operator=(TransformationRulePass const&) = delete;
        TransformationRulePass& operator=(TransformationRulePass&&) = default;
        /// @}

        /// Functions required by LLVM
        /// @{
        llvm::PreservedAnalyses run(llvm::Function& function, llvm::FunctionAnalysisManager& fam);
        static bool             isRequired();
        /// @}

      private:
        RuleSet      rule_set_{};
        Replacements replacements_; ///< Registered replacements to be executed.
    };

} // namespace quantum
} // namespace microsoft
