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
#include "Llvm/Llvm.hpp"
#include "Profile/Profile.hpp"
#include "RuleTransformationPass/Configuration.hpp"
#include "Rules/FactoryConfig.hpp"

#include <iomanip>
#include <iostream>
#include <unordered_map>

using namespace llvm;
using namespace microsoft::quantum;

int main(int argc, char **argv)
{
  try
  {
    auto generator = std::make_shared<DefaultProfileGenerator>();

    ConfigurationManager &configuration_manager = generator->configurationManager();
    configuration_manager.addConfig<QatConfig>();
    configuration_manager.addConfig<FactoryConfiguration>();

    // Parsing command line arguments
    ParameterParser parser;
    configuration_manager.setupArguments(parser);
    parser.parseArgs(argc, argv);
    configuration_manager.configure(parser);

    // Getting the main configuration
    auto const &config = configuration_manager.get<QatConfig>();

    // In case we debug, we also print the settings to allow provide a full
    // picture of what is going.
    if (config.dumpConfig())
    {
      configuration_manager.printConfiguration();
    }

    if (parser.arguments().empty())
    {
      std::cerr << "Usage: " << argv[0] << " [options] filename" << std::endl;
      configuration_manager.printHelp();
      std::cerr << "\n";
      exit(-1);
    }

    // Loading IR from file.
    LLVMContext  context;
    SMDiagnostic error;
    auto         module = parseIRFile(parser.getArg(0), error, context);

    if (!module)
    {
      std::cerr << "Invalid IR." << std::endl;
      exit(-1);
    }

    // Extracting commandline parameters

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

    // Checking if we are asked to generate a new QIR. If so, we will use
    // the profile to setup passes to
    auto profile = generator->newProfile(optimisation_level, config.debug());
    if (config.generate())
    {
      profile.apply(*module);

      // Priniting either human readible LL code if requested to do so.

      if (config.emitLlvm())
      {
        llvm::outs() << *module << "\n";
      }
    }

    // Verifying the module.
    if (config.verifyModule())
    {
      if (!profile.verify(*module))
      {
        llvm::outs() << "IR is broken."
                     << "\n";
        exit(-1);
      }
    }

    if (config.validate())
    {
      // Creating pass builder
      Profile analyser{config.debug()};

      // Creating a validation pass manager
      auto module_pass_manager = generator->createValidationModulePass(
          analyser.passBuilder(), optimisation_level, config.debug());
      module_pass_manager.run(*module, analyser.moduleAnalysisManager());
    }
  }
  catch (std::exception const &e)
  {
    llvm::outs() << "An error occured: " << e.what() << "\n";
  }

  return 0;
}
