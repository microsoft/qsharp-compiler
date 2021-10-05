// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// QIR Adaptor Tool (QAT)
///
/// QAT is a tool that helps the enduser to easily build and use new profiles. The tool provides a
/// commandline interface which is configurable through YAML files to validate a specific QIR
/// profile and generate a QIR profile compatible IR from a generic IR.
///
/// The tool itself make use of LLVM passes to perform analysis and transformations of the supplied
/// IR. These transfornations are described through high-level tasks such as
/// `useStaticQubitArrayAllocation`.
///
/// To provide an overview of the structure of this tool, we here provide a diagram showing the
/// relation between different instances in the program:
///
///
/// ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─
///            User input          │                  │      "Use" relation
/// └ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─                   ▼
///                 │  argc, argv
///                 ▼                                 ─ ─▶   "Produce" relation
/// ┌──────────────────────────────┐
/// │       ParameterParser        │◀─┐ Setup arguments
/// └──────────────────────────────┘  │
///    Load config  │                 │
///                 ▼                 │
/// ┌──────────────────────────────┐  │            ┌──────────────────────────────────┐
/// │     ConfigurationManager     │──┘    ┌ ─ ─ ─▶│             Ruleset              │
/// └──────────────────────────────┘               └──────────────────────────────────┘
///  Provide config │                      │                         │   Rules for
///                 ▼                                                ▼ transformation
/// ┌───────────────────────────────┐─ ─ ─ ┘       ┌──────────────────────────────────┐
/// │    DefaultProfileGenerator    │─ ─ ─ ─ ─ ─ ─▶│           RuleTransformationPass            │
/// └───────────────────────────────┘              └──────────────────────────────────┘
///                                                                  │  LLVM module
///                                                                  ▼      pass
/// ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─                ┌──────────────────────────────────┐
///              Output            │◀─ ─ ─ ─ ─ ─ ─ ┤  QAT / LLVM Module Pass Manager  │
/// └ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─      stdout    └──────────────────────────────────┘
///
///
///

#include "Apps/Qat/Config.hpp"
#include "Commandline/ConfigurationManager.hpp"
#include "Commandline/ParameterParser.hpp"
#include "Generators/DefaultProfileGenerator.hpp"
#include "Generators/LlvmPassesConfig.hpp"
#include "ModuleLoader/ModuleLoader.hpp"
#include "Profile/Profile.hpp"
#include "RuleTransformationPass/Configuration.hpp"
#include "RuleTransformationPass/RulePass.hpp"
#include "Rules/Factory.hpp"
#include "Rules/FactoryConfig.hpp"

#include "Llvm/Llvm.hpp"

#include <dlfcn.h>

#include <iomanip>
#include <iostream>
#include <unordered_map>

using namespace llvm;
using namespace microsoft::quantum;

int main(int argc, char** argv)
{
    try
    {
        // Default generator. A future version of QAT may allow the generator to be selected
        // through the command line, but it is hard coded for now.
        auto generator = std::make_shared<IProfileGenerator>();

        // Configuration and command line parsing
        //

        ConfigurationManager& configuration_manager = generator->configurationManager();
        configuration_manager.addConfig<QatConfig>();
        configuration_manager.addConfig<FactoryConfiguration>();

        ParameterParser parser;
        configuration_manager.setupArguments(parser);
        parser.parseArgs(argc, argv);
        configuration_manager.configure(parser);

        // Getting the main configuration
        auto config = configuration_manager.get<QatConfig>();

        // Loading components
        //

        generator->registerProfileComponent<RuleTransformationPassConfiguration>(
            "transformation-rules",
            [](RuleTransformationPassConfiguration const& cfg, IProfileGenerator* ptr, Profile& profile) {
                auto& ret = ptr->modulePassManager();

                // Default optimisation pipeline
                if (cfg.simplifyPriorTransform())
                {
                    auto&                   pass_builder = ptr->passBuilder();
                    llvm::ModulePassManager pipeline =
                        pass_builder.buildPerModuleDefaultPipeline(ptr->optimisationLevel());
                    ret.addPass(std::move(pipeline));
                }

                // Defining the mapping
                RuleSet rule_set;
                auto    factory =
                    RuleFactory(rule_set, profile.getQubitAllocationManager(), profile.getResultAllocationManager());
                factory.usingConfiguration(ptr->configurationManager().get<FactoryConfiguration>());

                // Creating profile pass
                ret.addPass(RuleTransformationPass(std::move(rule_set), cfg, &profile));
            });

        if (!config.load().empty())
        {
            // TODO (tfr): Add support for multiple loads
            void* handle = dlopen(config.load().c_str(), RTLD_LAZY);

            if (handle == nullptr)
            {
                std::cerr << "Invalid component " << config.load() << std::endl;
            }
            else
            {
                using LoadFunctionPtr = void (*)(IProfileGenerator*);
                LoadFunctionPtr load_component;
                load_component = reinterpret_cast<LoadFunctionPtr>(dlsym(handle, "loadComponent"));

                load_component(generator.get());
            }
        }

        generator->registerProfileComponent<LlvmPassesConfiguration>(
            "llvm-passes", [](LlvmPassesConfiguration const& cfg, IProfileGenerator* ptr, Profile&) {
                auto pass_pipeline = cfg.passPipeline();
                if (!pass_pipeline.empty())
                {

                    auto& pass_builder = ptr->passBuilder();
                    auto& npm          = ptr->modulePassManager();
                    if (!pass_builder.parsePassPipeline(npm, pass_pipeline, false, false))
                    {
                        throw std::runtime_error("Failed to set pass pipeline up.");
                    }
                }
                else if (cfg.alwaysInline())
                {
                    auto& ret          = ptr->modulePassManager();
                    auto& pass_builder = ptr->passBuilder();
                    ret.addPass(llvm::AlwaysInlinerPass());

                    auto inliner_pass = pass_builder.buildInlinerPipeline(
                        ptr->optimisationLevel(), llvm::PassBuilder::ThinLTOPhase::None, ptr->debug());
                    ret.addPass(std::move(inliner_pass));
                }
                else if (!cfg.disableDefaultPipeline())
                {
                    auto& mpm = ptr->modulePassManager();

                    // If not explicitly disabled, we fall back to the default LLVM pipeline
                    auto&                   pass_builder = ptr->passBuilder();
                    llvm::ModulePassManager pipeline1 =
                        pass_builder.buildPerModuleDefaultPipeline(ptr->optimisationLevel());
                    mpm.addPass(std::move(pipeline1));

                    llvm::ModulePassManager pipeline2 = pass_builder.buildModuleSimplificationPipeline(
                        ptr->optimisationLevel(), llvm::PassBuilder::ThinLTOPhase::None);
                    mpm.addPass(std::move(pipeline2));
                }
            });

        // Reconfiguring to get all the arguments of the passes registered
        parser.reset();
        configuration_manager.setupArguments(parser);
        parser.parseArgs(argc, argv);
        configuration_manager.configure(parser);

        // In case we debug, we also print the settings to allow provide a full
        // picture of what is going. This step deliberately comes before validating
        // the input to allow dumping the configuration if something goes wrong.
        if (config.dumpConfig())
        {
            configuration_manager.printConfiguration();
        }

        // Checking that we have sufficient information to proceed. If not we print
        // usage instructions and the corresponding description of how to use the tool.
        if (parser.arguments().empty())
        {
            std::cerr << "Usage: " << argv[0] << " [options] filename" << std::endl;
            configuration_manager.printHelp();
            std::cerr << "\n";
            exit(-1);
        }

        // Loading IR from file(s).
        //

        LLVMContext  context;
        auto         module = std::make_unique<Module>("qat-link", context);
        ModuleLoader loader(module.get());

        for (auto const& arg : parser.arguments())
        {
            if (!loader.addIrFile(arg))
            {
                llvm::errs() << "Could not load " << arg << "\n";
                return -1;
            }
        }

        if (!module)
        {
            std::cerr << "Invalid IR." << std::endl;
            exit(-1);
        }

        // Getting the optimisation level
        //
        auto optimisation_level = llvm::PassBuilder::OptimizationLevel::O0;

        // Setting the optimisation level
        if (config.opt1())
        {
            optimisation_level = llvm::PassBuilder::OptimizationLevel::O1;
        }

        if (config.opt2())
        {
            optimisation_level = llvm::PassBuilder::OptimizationLevel::O2;
        }

        if (config.opt3())
        {
            optimisation_level = llvm::PassBuilder::OptimizationLevel::O3;
        }

        // Profile manipulation
        //

        // Creating the profile that will be used for generation and validation
        auto profile = generator->newProfile(optimisation_level, config.debug());

        if (config.generate())
        {
            profile.apply(*module);
        }

        // We deliberately emit LLVM prior to verification and validation
        // to allow output the IR for debugging purposes.
        if (config.emitLlvm())
        {
            llvm::outs() << *module << "\n";
        }

        if (config.verifyModule())
        {
            if (!profile.verify(*module))
            {
                std::cerr << "IR is broken." << std::endl;
                exit(-1);
            }
        }

        if (config.validate())
        {
            if (!profile.validate(*module))
            {
                std::cerr << "QIR is not compliant with profile." << std::endl;
                exit(-1);
            }
        }
    }
    catch (std::exception const& e)
    {
        std::cerr << "An error occurred: " << e.what() << std::endl;
        exit(-1);
    }

    return 0;
}
