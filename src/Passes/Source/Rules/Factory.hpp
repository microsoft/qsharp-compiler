#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "Commandline/ConfigurationManager.hpp"
#include "Rules/FactoryConfig.hpp"
#include "Rules/ReplacementRule.hpp"
#include "Rules/RuleSet.hpp"

#include "Llvm/Llvm.hpp"

#include <memory>

namespace microsoft
{
namespace quantum
{

    /// Rule factory provides a high-level methods to build a ruleset that
    /// enforces certain aspects of QIR transformation.
    class RuleFactory
    {
      public:
        using String               = std::string;
        using ReplacementRulePtr   = std::shared_ptr<ReplacementRule>;
        using AllocationManagerPtr = IAllocationManager::AllocationManagerPtr;
        using Replacements         = ReplacementRule::Replacements;
        using Captures             = IOperandPrototype::Captures;
        using Instruction          = llvm::Instruction;
        using Value                = llvm::Value;
        using Builder              = ReplacementRule::Builder;

        /// Constructor configuration. Explicit construction with
        /// rule set to be configured, which can be moved using move
        /// semantics. No copy allowed.
        /// @{
        explicit RuleFactory(RuleSet& rule_set);
        RuleFactory()                   = delete;
        RuleFactory(RuleFactory const&) = delete;
        RuleFactory(RuleFactory&&)      = default;
        ~RuleFactory()                  = default;
        /// @}

        ///
        /// @{
        void usingConfiguration(FactoryConfiguration const& config);
        /// @}

        /// Generic rules
        /// @{
        /// Removes all calls to functions with a specified name.
        /// This function matches on name alone and ignores function
        /// arguments.
        void removeFunctionCall(String const& name);
        /// @}

        /// Conventions
        /// @{
        void useStaticQubitArrayAllocation();
        void useStaticQubitAllocation();
        void useStaticResultAllocation();
        /// @}

        /// Optimisations
        /// @{
        void optimiseBranchQuatumOne();
        void optimiseBranchQuatumZero();
        /// @}

        /// Disabling by feature
        /// @{
        void disableReferenceCounting();
        void disableAliasCounting();
        void disableStringSupport();
        /// @}

        void setDefaultIntegerWidth(uint32_t v);

      private:
        ReplacementRulePtr addRule(ReplacementRule&& rule);

        /// Affected artefacts
        /// @{
        RuleSet& rule_set_; ///< The ruleset we are building
        /// @}

        /// Allocation managers. Allocation managers for different types
        /// @{
        AllocationManagerPtr qubit_alloc_manager_{nullptr};
        AllocationManagerPtr result_alloc_manager_{nullptr};
        /// @}

        uint32_t default_integer_width_{64};
    };

} // namespace quantum
} // namespace microsoft
