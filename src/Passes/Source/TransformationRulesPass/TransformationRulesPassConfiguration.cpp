// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "TransformationRulesPass/TransformationRulesPassConfiguration.hpp"

namespace microsoft
{
namespace quantum
{

    void TransformationRulesPassConfiguration::setup(ConfigurationManager& config)
    {
        config.setSectionName("Pass configuration", "Configuration of the pass and its corresponding optimisations.");
        config.addParameter(delete_dead_code_, "delete-dead-code", "Deleted dead code.");
        config.addParameter(clone_functions_, "clone-functions", "Clone functions to ensure correct qubit allocation.");
        config.addParameter(
            transform_execution_path_only_, "transform-execution-path-only", "Transform execution paths only.");

        config.addParameter(
            max_recursion_, "max-recursion", "Defines the maximum recursion when unrolling the execution path");

        config.addParameter(
            assume_no_exceptions_, "assume-no-except", "Assumes that no exception will occur during runtime.");

        config.addParameter(reuse_qubits_, "reuse-qubits", "Use to define whether or not to reuse qubits.");
        config.addParameter(annotate_qubit_use_, "annotate-qubit-use", "Annotate the number of qubits used");

        config.addParameter(reuse_results_, "reuse-results", "Use to define whether or not to reuse results.");
        config.addParameter(annotate_result_use_, "annotate-result-use", "Annotate the number of results used");

        config.addParameter(
            entry_point_attr_, "entry-point-attr", "Specifies the attribute indicating the entry point.");

        config.addParameter(
            simplify_prior_transformation_, "simplify-prior-transform",
            "When active, the IR is simplified using LLVM passes before transformation.");

        // Not implemented yet
        config.addParameter(group_measurements_, "group-measurements", "NOT IMPLEMENTED - group-measurements");
        config.addParameter(one_shot_measurement_, "one-shot-measurement", "NOT IMPLEMENTED - one-shot-measurement");
    }

    TransformationRulesPassConfiguration TransformationRulesPassConfiguration::createDisabled()
    {
        TransformationRulesPassConfiguration ret;
        ret.delete_dead_code_              = false;
        ret.clone_functions_               = false;
        ret.transform_execution_path_only_ = false;
        ret.max_recursion_                 = 512;
        ret.simplify_prior_transformation_ = false;
        ret.reuse_qubits_                  = false;
        ret.annotate_qubit_use_            = false;
        ret.group_measurements_            = false;
        ret.one_shot_measurement_          = false;
        return ret;
    }

    bool TransformationRulesPassConfiguration::shouldSimplifyPriorTransform() const
    {
        return simplify_prior_transformation_;
    }

    bool TransformationRulesPassConfiguration::shouldDeleteDeadCode() const
    {
        return delete_dead_code_;
    }

    bool TransformationRulesPassConfiguration::shouldCloneFunctions() const
    {
        return clone_functions_;
    }

    bool TransformationRulesPassConfiguration::shouldTransformExecutionPathOnly() const
    {
        return transform_execution_path_only_;
    }

    uint64_t TransformationRulesPassConfiguration::maxRecursion() const
    {
        return max_recursion_;
    }

    bool TransformationRulesPassConfiguration::shouldReuseQubits() const
    {
        return reuse_qubits_;
    }

    bool TransformationRulesPassConfiguration::shouldAnnotateQubitUse() const
    {
        return annotate_qubit_use_;
    }

    bool TransformationRulesPassConfiguration::shouldReuseResults() const
    {
        return reuse_results_;
    }

    bool TransformationRulesPassConfiguration::shouldAnnotateResultUse() const
    {
        return annotate_result_use_;
    }

    bool TransformationRulesPassConfiguration::shouldGroupMeasurements() const
    {
        return group_measurements_;
    }

    bool TransformationRulesPassConfiguration::oneShotMeasurement() const
    {
        return one_shot_measurement_;
    }

    std::string TransformationRulesPassConfiguration::entryPointAttr() const
    {
        return entry_point_attr_;
    }

    bool TransformationRulesPassConfiguration::assumeNoExceptions() const
    {
        return assume_no_exceptions_;
    }

    bool TransformationRulesPassConfiguration::isDisabled() const
    {
        return (
            delete_dead_code_ == false && clone_functions_ == false && simplify_prior_transformation_ == false &&
            transform_execution_path_only_ == false && reuse_qubits_ == false && group_measurements_ == false &&
            one_shot_measurement_ == false);
    }

    bool TransformationRulesPassConfiguration::operator==(TransformationRulesPassConfiguration const& ref) const
    {

        return (
            delete_dead_code_ == ref.delete_dead_code_ && clone_functions_ == ref.clone_functions_ &&
            transform_execution_path_only_ == ref.transform_execution_path_only_ &&
            reuse_qubits_ == ref.reuse_qubits_ && group_measurements_ == ref.group_measurements_ &&
            one_shot_measurement_ == ref.one_shot_measurement_);
    }

} // namespace quantum
} // namespace microsoft
