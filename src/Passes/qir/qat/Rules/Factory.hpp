#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "Commandline/ConfigurationManager.hpp"
#include "Rules/FactoryConfig.hpp"
#include "Rules/ReplacementRule.hpp"
#include "Rules/RuleSet.hpp"
#include "Types/Types.hpp"

#include "Llvm/Llvm.hpp"

#include <memory>

namespace microsoft
{
namespace quantum
{

    /// Rule factory provides a high-level methods to build a rule set that
    /// enforces certain aspects of QIR transformation.
    class RuleFactory
    {
      public:
        /// ReplacementRule pointer type used for the construction of replacement rules
        using ReplacementRulePtr = std::shared_ptr<ReplacementRule>;

        /// Allocation manager pointer used to hold allocation managers
        using AllocationManagerPtr = IAllocationManager::AllocationManagerPtr;

        // Constructor configuration. Explicit construction with
        // rule set to be configured, which can be moved using move
        // semantics. No copy allowed.
        //

        RuleFactory(
            RuleSet&             rule_set,
            AllocationManagerPtr qubit_alloc_manager,
            AllocationManagerPtr result_alloc_manager);
        RuleFactory()                   = delete;
        RuleFactory(RuleFactory const&) = delete;
        RuleFactory(RuleFactory&&)      = default;
        ~RuleFactory()                  = default;

        //
        //

        /// This takes a FactoryConfiguration as argument and enable rules accordingly.
        void usingConfiguration(FactoryConfiguration const& config);

        // Generic rules
        //

        /// Removes all calls to functions with a specified name.
        /// This function matches on name alone and ignores function
        /// arguments.
        void removeFunctionCall(String const& name);

        // Conventions
        //

        /// Static qubit array allocation identifies allocations, array access and releases. Each of these
        /// are replaced with static values. Patterns recognised include
        ///
        /// ```
        /// %array = call %Array* @__quantum__rt__qubit_allocate_array(i64 10)
        /// ```
        ///
        /// which is replaced by a constant pointer
        ///
        /// ```
        /// %array = inttoptr i64 0 to %Array*
        /// ```
        ///
        /// The array allocation is managed through the qubit allocation manager. Access to qubit arrays
        ///
        /// ```
        /// %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array, i64 7)
        /// %1 = bitcast i8* %0 to %Qubit**
        /// %qubit = load %Qubit*, %Qubit** %1, align 8
        /// ```
        ///
        /// is replaced by off-setting the array value by 7 to get
        ///
        /// ```
        /// %qubit = inttoptr i64 7 to %Qubit*
        /// ```
        ///
        /// Finally, release is recognised and the allocation manager is invoked accordingly.
        void useStaticQubitArrayAllocation();

        /// Static qubit allocation identifies allocation and release of single qubits. It uses the qubit
        /// allocation manager to track allocation and releases of qubits. It translates
        ///
        /// ```
        /// %qubit1 = call %Qubit* @__quantum__rt__qubit_allocate()
        /// %qubit2 = call %Qubit* @__quantum__rt__qubit_allocate()
        /// %qubit3 = call %Qubit* @__quantum__rt__qubit_allocate()
        /// %qubit4 = call %Qubit* @__quantum__rt__qubit_allocate()
        /// %qubit5 = call %Qubit* @__quantum__rt__qubit_allocate()
        /// ```
        ///
        /// to
        ///
        /// ```
        /// %qubit1 = inttoptr i64 0 to %Qubit*
        /// %qubit2 = inttoptr i64 1 to %Qubit*
        /// %qubit3 = inttoptr i64 2 to %Qubit*
        /// %qubit4 = inttoptr i64 3 to %Qubit*
        /// %qubit5 = inttoptr i64 4 to %Qubit*
        /// ```
        /// if the BasicAllocationManager is used.
        void useStaticQubitAllocation();

        /// Static allocation of results. This feature is similar to `useStaticQubitAllocation` but uses
        /// the result allocation manager.
        void useStaticResultAllocation();

        void resolveConstantArraySizes();

        void inlineCallables();

        // Optimisations
        //

        /// Replaces branching of quantum results compared to one. This is a relatively advanced pattern,
        /// intended for base profile-like constructs where
        ///
        /// ```
        /// %1 = tail call %Result* @__quantum__rt__result_get_one()
        /// %2 = tail call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
        /// br i1 %2, label %then0__1, label %continue__1
        /// ```
        ///
        /// is mapped into
        ///
        /// ```
        /// %1 = call i1 @__quantum__qis__read_result__body(%Result* %0)
        /// br i1 %1, label %then0__1, label %continue__1
        /// ```
        ///
        /// which removes the need for constant one.
        void optimiseResultOne();

        /// Replaces branching of quantum results compared to zero. This method is not implemented yet.
        void optimiseResultZero();

        // Disabling by feature
        //

        /// This method disables reference counting for arrays, strings and results. It does so by simply
        /// removing the instructions and the resulting code is expected to be executed either on a stack
        /// VM or with shared pointer logic.
        void disableReferenceCounting();

        /// This method disables alias counting for arrays, strings and results.
        void disableAliasCounting();

        /// Removes string support by removing string related instructions. At the moment these include
        /// `__quantum__rt__string_create`,
        /// `__quantum__rt__string_update_reference_count`, `__quantum__rt__string_update_alias_count` and
        /// `__quantum__rt__message`.
        void disableStringSupport();

        // Configuration
        //

        /// Sets the integer width used when it cannot be deducted from the context of the transformation.
        void setDefaultIntegerWidth(uint32_t v);

      private:
        /// Helper function that moves a replacement rule into a shared pointer, adds it to the rule set
        /// and returns a copy of it.
        ReplacementRulePtr addRule(ReplacementRule&& rule);

        // Affected artefacts
        //

        RuleSet& rule_set_; ///< The rule set we are building

        // Allocation managers.
        //

        /// Qubit allocation manager which is used in the case of static qubit allocation.
        AllocationManagerPtr qubit_alloc_manager_{nullptr};

        /// Result allocation manager which is used in the case of static results allocation.
        AllocationManagerPtr result_alloc_manager_{nullptr};

        /// Configuration
        //

        /// The default integer width. This value is used whenever the width within the context cannot be
        /// inferred.
        uint32_t default_integer_width_{64};
    };

} // namespace quantum
} // namespace microsoft
