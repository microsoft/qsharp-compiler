// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "RuleTransformationPass/Configuration.hpp"

namespace microsoft
{
namespace quantum
{

    void RuleTransformationPassConfiguration::setup(ConfigurationManager& config)
    {
        config.setSectionName("Pass configuration", "Configuration of the pass and its corresponding optimisations.");
        config.addParameter(delete_dead_code_, "delete-dead-code", "Deleted dead code.");
        config.addParameter(clone_functions_, "clone-functions", "Clone functions to ensure correct qubit allocation.");

        config.addParameter(
            max_recursion_, "max-recursion", "Defines the maximum recursion when unrolling the execution path");

        config.addParameter(reuse_qubits_, "reuse-qubits", "Use to define whether or not to reuse qubits.");
        config.addParameter(annotate_qubit_use_, "annotate-qubit-use", "Annotate the number of qubits used");

        config.addParameter(
            entry_point_attr_, "entry-point-attr", "Specifies the attribute indicating the entry point.");

        // Not implemented yet
        config.addParameter(group_measurements_, "group-measurements", "NOT IMPLEMENTED - group-measurements");
        config.addParameter(one_shot_measurement_, "one-shot-measurement", "NOT IMPLEMENTED - one-shot-measurement");
    }

    RuleTransformationPassConfiguration RuleTransformationPassConfiguration::disable()
    {
        RuleTransformationPassConfiguration ret;
        ret.delete_dead_code_              = false;
        ret.clone_functions_               = false;
        ret.transform_execution_path_only_ = false;
        ret.max_recursion_                 = 512;
        ret.reuse_qubits_                  = false;
        ret.annotate_qubit_use_            = false;
        ret.group_measurements_            = false;
        ret.one_shot_measurement_          = false;
        return ret;
    }

    bool RuleTransformationPassConfiguration::deleteDeadCode() const
    {
        return delete_dead_code_;
    }

    bool RuleTransformationPassConfiguration::cloneFunctions() const
    {
        return clone_functions_;
    }

    bool RuleTransformationPassConfiguration::transformExecutionPathOnly() const
    {
        return transform_execution_path_only_;
    }

    uint64_t RuleTransformationPassConfiguration::maxRecursion() const
    {
        return max_recursion_;
    }

    bool RuleTransformationPassConfiguration::reuseQubits() const
    {
        return reuse_qubits_;
    }

    bool RuleTransformationPassConfiguration::annotateQubitUse() const
    {
        return annotate_qubit_use_;
    }

    bool RuleTransformationPassConfiguration::groupMeasurements() const
    {
        return group_measurements_;
    }

    bool RuleTransformationPassConfiguration::oneShotMeasurement() const
    {
        return one_shot_measurement_;
    }

    std::string RuleTransformationPassConfiguration::entryPointAttr() const
    {
        return entry_point_attr_;
    }

    bool RuleTransformationPassConfiguration::isDisabled() const
    {

        return (
            delete_dead_code_ == false && clone_functions_ == false && transform_execution_path_only_ == false &&
            reuse_qubits_ == false && group_measurements_ == false && one_shot_measurement_ == false);
    }

    bool RuleTransformationPassConfiguration::isDefault() const
    {
        RuleTransformationPassConfiguration ref{};

        return (
            delete_dead_code_ == ref.delete_dead_code_ && clone_functions_ == ref.clone_functions_ &&
            transform_execution_path_only_ == ref.transform_execution_path_only_ &&
            reuse_qubits_ == ref.reuse_qubits_ && group_measurements_ == ref.group_measurements_ &&
            one_shot_measurement_ == ref.one_shot_measurement_);
    }

} // namespace quantum
} // namespace microsoft
