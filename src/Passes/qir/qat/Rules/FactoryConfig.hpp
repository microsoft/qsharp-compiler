#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft
{
namespace quantum
{

    class FactoryConfiguration
    {
      public:
        void setup(ConfigurationManager& config)
        {
            config.setSectionName("Transformation rules", "Rules used to transform instruction sequences in the QIR.");
            config.addParameter(
                disable_reference_counting_, "disable-reference-counting",
                "Disables reference counting by instruction removal.");

            config.addParameter(
                disable_reference_counting_, "disable-reference-counting",
                "Disables reference counting by instruction removal.");
            config.addParameter(
                disable_alias_counting_, "disable-alias-counting", "Disables alias counting by instruction removal.");
            config.addParameter(
                disable_string_support_, "disable-string-support", "Disables string support by instruction removal.");
            config.addParameter(
                optimise_result_one_, "optimise-result-one",
                "Maps branching based on quantum measurements compared to one to base profile "
                "type measurement.");
            config.addParameter(
                optimise_result_zero_, "optimise-result-zero",
                "Maps branching based on quantum measurements compared to zero to base profile "
                "type measurement.");
            config.addParameter(
                use_static_qubit_array_allocation_, "use-static-qubit-array-allocation",
                "Maps allocation of qubit arrays to static array allocation.");
            config.addParameter(
                use_static_qubit_allocation_, "use-static-qubit-allocation",
                "Maps qubit allocation to static allocation.");
            config.addParameter(
                use_static_result_allocation_, "use-static-result-allocation",
                "Maps result allocation to static allocation.");
        }

        static FactoryConfiguration createDisabled()
        {
            FactoryConfiguration ret;
            ret.disable_reference_counting_        = false;
            ret.disable_alias_counting_            = false;
            ret.disable_string_support_            = false;
            ret.optimise_result_one_               = false;
            ret.optimise_result_zero_              = false;
            ret.use_static_qubit_array_allocation_ = false;
            ret.use_static_qubit_allocation_       = false;
            ret.use_static_result_allocation_      = false;
            return ret;
        }

        bool disableReferenceCounting() const
        {
            return disable_reference_counting_;
        }

        bool disableAliasCounting() const
        {
            return disable_alias_counting_;
        }

        bool disableStringSupport() const
        {
            return disable_string_support_;
        }

        bool optimiseResultOne() const
        {
            return optimise_result_one_;
        }

        bool optimiseResultZero() const
        {
            return optimise_result_zero_;
        }

        bool useStaticQubitArrayAllocation() const
        {
            return use_static_qubit_array_allocation_;
        }

        bool useStaticQubitAllocation() const
        {
            return use_static_qubit_allocation_;
        }

        bool useStaticResultAllocation() const
        {
            return use_static_result_allocation_;
        }

        uint32_t defaultIntegerWidth() const
        {
            return default_integer_width_;
        }

        bool isDisabled() const
        {
            return (
                disable_reference_counting_ == false && disable_alias_counting_ == false &&
                disable_string_support_ == false && optimise_result_one_ == false && optimise_result_zero_ == false &&
                use_static_qubit_array_allocation_ == false && use_static_qubit_allocation_ == false &&
                use_static_result_allocation_ == false);
        }

        bool isDefault() const
        {
            FactoryConfiguration ref{};

            return (
                disable_reference_counting_ == ref.disable_reference_counting_ &&
                disable_alias_counting_ == ref.disable_alias_counting_ &&
                disable_string_support_ == ref.disable_string_support_ &&
                optimise_result_one_ == ref.optimise_result_one_ &&
                optimise_result_zero_ == ref.optimise_result_zero_ &&
                use_static_qubit_array_allocation_ == ref.use_static_qubit_array_allocation_ &&
                use_static_qubit_allocation_ == ref.use_static_qubit_allocation_ &&
                use_static_result_allocation_ == ref.use_static_result_allocation_);
        }

      private:
        /// Factory Configuration
        /// @{
        bool disable_reference_counting_{true};
        bool disable_alias_counting_{true};
        bool disable_string_support_{true};
        /// @}

        /// Optimisations
        /// @{
        bool optimise_result_one_{true};
        bool optimise_result_zero_{true};
        /// @}

        bool use_static_qubit_array_allocation_{true};
        bool use_static_qubit_allocation_{true};
        bool use_static_result_allocation_{true};

        uint32_t default_integer_width_{64};
    };

} // namespace quantum
} // namespace microsoft
