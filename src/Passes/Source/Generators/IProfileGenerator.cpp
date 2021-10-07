// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/IProfileGenerator.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    Profile IProfileGenerator::newProfile(OptimizationLevel const& optimisation_level, bool debug)
    {
        Profile ret{debug};

        auto module_pass_manager = createGenerationModulePass(ret, optimisation_level, debug);

        ret.setModulePassManager(std::move(module_pass_manager));

        return ret;
    }

    llvm::ModulePassManager IProfileGenerator::createGenerationModulePass(
        Profile&                 profile,
        OptimizationLevel const& optimisation_level,
        bool                     debug)
    {
        auto&                   pass_builder          = profile.passBuilder();
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
                llvm::outs() << "Setting " << c.first << " up\n";
            }

            c.second(this, profile);
        }

        return ret;
    }

    llvm::ModulePassManager IProfileGenerator::createValidationModulePass(PassBuilder&, OptimizationLevel const&, bool)
    {
        throw std::runtime_error("Validation is not supported yet.");
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

    ConfigurationManager const& IProfileGenerator::configurationManager() const
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
