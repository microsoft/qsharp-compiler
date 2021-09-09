// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/IProfileGenerator.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    IProfileGenerator::IProfileGenerator()  = default;
    IProfileGenerator::~IProfileGenerator() = default;

    llvm::ModulePassManager IProfileGenerator::createGenerationModulePass(
        PassBuilder&             pass_builder,
        OptimizationLevel const& optimisation_level,
        bool                     debug)
    {
        llvm::ModulePassManager ret                   = pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
        auto                    function_pass_manager = pass_builder.buildFunctionSimplificationPipeline(
            optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);

        module_pass_manager_ = &ret;
        pass_builder_        = &pass_builder;
        optimisation_level_  = optimisation_level;
        debug_               = debug;

        for (auto& c : components_)
        {
            if (debug)
            {
                llvm::errs() << "Setting " << c.first << " up\n";
            }

            c.second(this);
        }

        return ret;
    }

    llvm::ModulePassManager& IProfileGenerator::modulePassManager()
    {
        return *module_pass_manager_;
    }

    llvm::PassBuilder& IProfileGenerator::passBuilder()
    {
        return *pass_builder_;
    }

    ConfigurationManager& IProfileGenerator::configurationManager()
    {
        return configuration_manager_;
    }

    IProfileGenerator::OptimizationLevel IProfileGenerator::optimisationLevel() const
    {
        return optimisation_level_;
    }

    bool IProfileGenerator::debug() const
    {
        return debug_;
    }

} // namespace quantum
} // namespace microsoft
