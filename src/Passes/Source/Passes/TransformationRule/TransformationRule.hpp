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

        /// Construction and destruction configuration.
        /// @{

        /// Constructor which creates a pass with a given set of rules.
        explicit TransformationRulePass(RuleSet&& rule_set);

        /// Default construction is not permitted.
        TransformationRulePass() = delete;

        /// Copy construction is banned.
        TransformationRulePass(TransformationRulePass const&) = delete;

        /// We allow move semantics.
        TransformationRulePass(TransformationRulePass&&) = default;

        /// Default destruction.
        ~TransformationRulePass() = default;
        /// @}

        /// Operators
        /// @{

        /// Copy assignment is banned.
        TransformationRulePass& operator=(TransformationRulePass const&) = delete;

        /// Move assignement is permitted.
        TransformationRulePass& operator=(TransformationRulePass&&) = default;
        /// @}

        /// Functions required by LLVM
        /// @{

        /// Implements the transformation analysis which uses the supplied ruleset to make substitutions
        /// in each function.
        llvm::PreservedAnalyses run(llvm::Function& function, llvm::FunctionAnalysisManager& fam);

        /// Whether or not this pass is required to run.
        static bool isRequired();
        /// @}

      private:
        RuleSet      rule_set_{};
        Replacements replacements_; ///< Registered replacements to be executed.
    };

} // namespace quantum
} // namespace microsoft
