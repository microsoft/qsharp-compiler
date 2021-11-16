// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/ProfileGenerator.hpp"
#include "TransformationRulesPass/TransformationRulesPassConfiguration.hpp"
#include "ValidationPass/ValidationPassConfiguration.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    Profile ProfileGenerator::newProfile(String const& name, OptimizationLevel const& optimisation_level, bool debug)
    {
        auto qubit_allocation_manager  = BasicAllocationManager::createNew();
        auto result_allocation_manager = BasicAllocationManager::createNew();

        auto cfg = configuration_manager_.get<TransformationRulesPassConfiguration>();
        qubit_allocation_manager->setReuseRegisters(cfg.shouldReuseQubits());
        result_allocation_manager->setReuseRegisters(cfg.shouldReuseResults());

        // Creating profile
        // TODO(tfr): Set target machine
        Profile ret{name, debug, nullptr, qubit_allocation_manager, result_allocation_manager};

        auto module_pass_manager = createGenerationModulePassManager(ret, optimisation_level, debug);

        ret.setModulePassManager(std::move(module_pass_manager));

        // Creating validator
        auto validator = std::make_unique<Validator>(configuration_manager_.get<ValidationPassConfiguration>(), debug);
        ret.setValidator(std::move(validator));

        return ret;
    }

    llvm::ModulePassManager ProfileGenerator::createGenerationModulePassManager(
        Profile&                 profile,
        OptimizationLevel const& optimisation_level,
        bool                     debug)
    {
        auto&                   pass_builder = profile.passBuilder();
        llvm::ModulePassManager ret{};

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

    llvm::ModulePassManager ProfileGenerator::createValidationModulePass(PassBuilder&, OptimizationLevel const&, bool)
    {
        throw std::runtime_error("Validation is not supported yet.");
    }

    llvm::ModulePassManager& ProfileGenerator::modulePassManager()
    {
        return *module_pass_manager_;
    }

    llvm::PassBuilder& ProfileGenerator::passBuilder()
    {
        return *pass_builder_;
    }

    ConfigurationManager& ProfileGenerator::configurationManager()
    {
        return configuration_manager_;
    }

    ConfigurationManager const& ProfileGenerator::configurationManager() const
    {
        return configuration_manager_;
    }

    ProfileGenerator::OptimizationLevel ProfileGenerator::optimisationLevel() const
    {
        return optimisation_level_;
    }

    bool ProfileGenerator::isDebugMode() const
    {
        return debug_;
    }

} // namespace quantum
} // namespace microsoft
