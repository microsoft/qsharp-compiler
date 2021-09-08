// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "ProfilePass/Configuration.hpp"

namespace microsoft
{
namespace quantum
{

    void ProfilePassConfiguration::setup(ConfigurationManager& config)
    {
        config.setSectionName("Pass configuration", "Configuration of the pass and its corresponding optimisations.");
        config.addParameter(delete_dead_code_, "delete-dead-code", "Deleted dead code.");
        config.addParameter(clone_functions_, "clone-functions", "Clone functions to ensure correct qubit allocation.");

        config.addParameter(max_recursion_, "max-recursion", "max-recursion");
        config.addParameter(reuse_qubits_, "reuse-qubits", "reuse-qubits");

        config.addParameter(
            entry_point_attr_, "entry-point-attr", "Specifies the attribute indicating the entry point.");

        // Not implemented yet
        config.addParameter(group_measurements_, "group-measurements", "NOT IMPLEMENTED - group-measurements");
        config.addParameter(one_shot_measurement_, "one-shot-measurement", "NOT IMPLEMENTED - one-shot-measurement");
    }

    ProfilePassConfiguration ProfilePassConfiguration::disable()
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

    bool ProfilePassConfiguration::deleteDeadCode() const
    {
        return delete_dead_code_;
    }

    bool ProfilePassConfiguration::cloneFunctions() const
    {
        return clone_functions_;
    }

    bool ProfilePassConfiguration::transformExecutionPathOnly() const
    {
        return transform_execution_path_only_;
    }

    uint64_t ProfilePassConfiguration::maxRecursion() const
    {
        return max_recursion_;
    }

    bool ProfilePassConfiguration::reuseQubits() const
    {
        return reuse_qubits_;
    }

    bool ProfilePassConfiguration::groupMeasurements() const
    {
        return group_measurements_;
    }

    bool ProfilePassConfiguration::oneShotMeasurement() const
    {
        return one_shot_measurement_;
    }

    std::string ProfilePassConfiguration::entryPointAttr() const
    {
        return entry_point_attr_;
    }

    bool ProfilePassConfiguration::isDisabled() const
    {

        return (
            delete_dead_code_ == false && clone_functions_ == false && transform_execution_path_only_ == false &&
            reuse_qubits_ == false && group_measurements_ == false && one_shot_measurement_ == false);
    }

    bool ProfilePassConfiguration::isDefault() const
    {
        ProfilePassConfiguration ref{};

        return (
            delete_dead_code_ == ref.delete_dead_code_ && clone_functions_ == ref.clone_functions_ &&
            transform_execution_path_only_ == ref.transform_execution_path_only_ &&
            reuse_qubits_ == ref.reuse_qubits_ && group_measurements_ == ref.group_measurements_ &&
            one_shot_measurement_ == ref.one_shot_measurement_);
    }

} // namespace quantum
} // namespace microsoft
