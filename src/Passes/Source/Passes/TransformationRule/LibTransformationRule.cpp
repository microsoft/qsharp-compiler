// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Rules/Factory.hpp"

#include "Llvm/Llvm.hpp"

#include <fstream>
#include <iostream>

namespace
{
llvm::PassPluginLibraryInfo getTransformationRulePluginInfo()
{
    using namespace microsoft::quantum;
    using namespace llvm;

    return {LLVM_PLUGIN_API_VERSION, "TransformationRule", LLVM_VERSION_STRING, [](PassBuilder& pb) {
                // Registering the pass
                pb.registerPipelineParsingCallback(
                    [](StringRef name, FunctionPassManager& fpm, ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
                        // Base profile
                        if (name == "restrict-qir<base-profile>")
                        {
                            // Defining a harded coded set of rules as LLVM does not provide means
                            // to configure passes through opt.
                            RuleSet rule_set;

                            // Defining the mapping
                            auto factory = RuleFactory(rule_set);

                            factory.useStaticQubitArrayAllocation();
                            factory.useStaticQubitAllocation();
                            factory.useStaticResultAllocation();

                            factory.optimiseBranchQuatumOne();
                            //  factory.optimiseBranchQuatumZero();

                            factory.disableReferenceCounting();
                            factory.disableAliasCounting();
                            factory.disableStringSupport();

                            fpm.addPass(TransformationRulePass(std::move(rule_set)));
                            return true;
                        }

                        return false;
                    });
            }};
}
} // namespace

// Interface for loading the plugin
extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
    return getTransformationRulePluginInfo();
}
