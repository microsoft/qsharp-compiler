#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft
{
namespace quantum
{

    class ProfilePassConfiguration
    {
      public:
        void setup(ConfigurationManager& config)
        {
            config.setSectionName(
                "Pass configuration", "Configuration of the pass and its corresponding optimisations.");
            config.addParameter(delete_dead_code_, "delete-dead-code", "Deleted dead code.");
            config.addParameter(
                clone_functions_, "clone-functions", "Clone functions to ensure correct qubit allocation.");

            config.addParameter(max_recursion_, "max-recursion", "max-recursion");
            config.addParameter(reuse_qubits_, "reuse-qubits", "reuse-qubits");

            config.addParameter(
                entry_point_attr_, "entry-point-attr", "Specifies the attribute indicating the entry point.");

            // Not implemented yet
            config.addParameter(group_measurements_, "group-measurements", "NOT IMPLEMENTED - group-measurements");
            config.addParameter(
                one_shot_measurement_, "one-shot-measurement", "NOT IMPLEMENTED - one-shot-measurement");
        }

        static ProfilePassConfiguration disable()
        {
            ProfilePassConfiguration ret;
            ret.delete_dead_code_              = false;
            ret.clone_functions_               = false;
            ret.transform_execution_path_only_ = false;
            ret.max_recursion_                 = 512;
            ret.reuse_qubits_                  = false;
            ret.group_measurements_            = false;
            ret.one_shot_measurement_          = false;
            return ret;
        }

        bool deleteDeadCode() const
        {
            return delete_dead_code_;
        }

        bool cloneFunctions() const
        {
            return clone_functions_;
        }

        bool transformExecutionPathOnly() const
        {
            return transform_execution_path_only_;
        }

        uint64_t maxRecursion() const
        {
            return max_recursion_;
        }

        bool reuseQubits() const
        {
            return reuse_qubits_;
        }

        bool groupMeasurements() const
        {
            return group_measurements_;
        }

        bool oneShotMeasurement() const
        {
            return one_shot_measurement_;
        }

        std::string entryPointAttr() const
        {
            return entry_point_attr_;
        }

      private:
        /// @{
        bool delete_dead_code_{true};
        bool clone_functions_{true};
        bool transform_execution_path_only_{true};
        /// @}

        /// Const-expression
        /// @{
        uint64_t max_recursion_{512};
        /// @}

        /// Allocation options
        /// @{
        bool reuse_qubits_{true}; // NOT IMPLEMENTED
        /// @}

        /// Measurement
        /// @{
        bool group_measurements_{false};  // NOT IMPLEMENTED
        bool one_shot_measurement_{true}; // NOT IMPLEMENTED
                                          /// @}

        std::string entry_point_attr_{"EntryPoint"};
    };

} // namespace quantum
} // namespace microsoft
