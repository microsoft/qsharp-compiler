#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/IAllocationManager.hpp"
#include "Rules/IOperandPrototype.hpp"
#include "Rules/ReplacementRule.hpp"

#include "Llvm/Llvm.hpp"

#include <memory>
#include <vector>

namespace microsoft
{
namespace quantum
{

    /// RuleSet contains a set of replacement rules and the corresponding logic
    /// to apply the rules. The class allows one to apply the rules by which
    /// each rule is tested one-by-one until a successful attempt at performing
    /// a replace has happened, or the list was exhausted.
    class RuleSet
    {
      public:
        using ReplacementRulePtr   = std::shared_ptr<ReplacementRule>;
        using Rules                = std::vector<ReplacementRulePtr>;
        using Replacements         = ReplacementRule::Replacements;
        using Captures             = IOperandPrototype::Captures;
        using Instruction          = llvm::Instruction;
        using Value                = llvm::Value;
        using Builder              = ReplacementRule::Builder;
        using AllocationManagerPtr = IAllocationManager::AllocationManagerPtr;

        // Constructors
        //
        RuleSet()               = default;
        RuleSet(RuleSet const&) = default;
        RuleSet(RuleSet&&)      = default;
        ~RuleSet()              = default;

        // Operators
        //

        RuleSet& operator=(RuleSet const&) = default;
        RuleSet& operator=(RuleSet&&) = default;

        // Operating rule sets
        //

        /// Matches patterns and runs the replacement routines if a match
        /// is found. The function returns true if a pattern is matched and
        /// and the replacement was a success. In all other cases, it returns
        /// false.
        bool matchAndReplace(Instruction* value, Replacements& replacements);

        // Set up and configuration
        //

        /// Adds a new replacement rule to the set.
        void addRule(ReplacementRulePtr const& rule);
        void addRule(ReplacementRule&& rule);

        /// Clears the rule set for all rules.
        void clear();

        /// Returns the size of the rule set.
        uint64_t size() const;

      private:
        Rules rules_; ///< Rules that describes QIR mappings
    };

} // namespace quantum
} // namespace microsoft
