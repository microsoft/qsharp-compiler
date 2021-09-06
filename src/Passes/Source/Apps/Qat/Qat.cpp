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
/// │       ProfileGenerator        │─ ─ ─ ─ ─ ─ ─▶│           ProfilePass            │
/// └───────────────────────────────┘              └──────────────────────────────────┘
///                                                                  │  LLVM module
///                                                                  ▼      pass
/// ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─                ┌──────────────────────────────────┐
///              Output            │◀─ ─ ─ ─ ─ ─ ─ ┤  QAT / LLVM Module Pass Manager  │
/// └ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─      stdout    └──────────────────────────────────┘
///
///

#include "Apps/Qat/Config.hpp"
#include "Apps/Qat/LlvmAnalysis.hpp"
#include "Commandline/ConfigurationManager.hpp"
#include "Commandline/ParameterParser.hpp"
#include "Llvm/Llvm.hpp"
#include "ProfilePass/Configuration.hpp"
#include "Profiles/IProfile.hpp"
#include "Profiles/LlvmPassesConfig.hpp"
#include "Profiles/ProfileGenerator.hpp"
#include "Profiles/RuleSetProfile.hpp"
#include "Rules/FactoryConfig.hpp"

#include <iomanip>
#include <iostream>
#include <unordered_map>

using namespace llvm;
using namespace microsoft::quantum;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wweak-template-vtables"
namespace microsoft {
namespace quantum {
template class ConfigBind<bool>;
IConfigBind::~IConfigBind() = default;
}  // namespace quantum
}  // namespace microsoft
#pragma clang diagnostic pop

int main(int argc, char **argv)
{
  try
  {

    ConfigurationManager configuration_manager;
    configuration_manager.addConfig<QatConfig>();
    configuration_manager.addConfig<FactoryConfiguration>();
    configuration_manager.addConfig<ProfilePassConfiguration>();
    configuration_manager.addConfig<LlvmPassesConfiguration>();

    // Parsing command line arguments
    ParameterParser parser;
    configuration_manager.setupArguments(parser);
    parser.parseArgs(argc, argv);
    configuration_manager.configure(parser);

    // Getting the main configuration
    auto const &config = configuration_manager.get<QatConfig>();

    // In case we debug, we also print the settings to allow provide a full
    // picture of what is going.
    if (config.debug)
    {
      // TODO: Dump config
    }

    if (parser.arguments().empty())
    {
      std::cerr << "Usage: " << argv[0] << " [options] filename" << std::endl;
      configuration_manager.printHelp();
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

    auto                      optimisation_level = llvm::PassBuilder::OptimizationLevel::O0;
    std::shared_ptr<IProfile> profile = std::make_shared<ProfileGenerator>(configuration_manager);

    // Setting the optimisation level
    if (config.opt1)
    {
      optimisation_level = llvm::PassBuilder::OptimizationLevel::O1;
    }

    if (config.opt2)
    {
      optimisation_level = llvm::PassBuilder::OptimizationLevel::O2;
    }

    if (config.opt3)
    {
      optimisation_level = llvm::PassBuilder::OptimizationLevel::O3;
    }

    // Checking if we are asked to generate a new QIR. If so, we will use
    // the profile to setup passes to
    if (config.generate)
    {
      // Creating pass builder
      LlvmAnalyser analyser{config.debug};

      // Preparing pass for generation based on profile
      profile->addFunctionAnalyses(analyser.functionAnalysisManager());
      auto module_pass_manager = profile->createGenerationModulePass(
          analyser.passBuilder(), optimisation_level, config.debug);

      // Running the pass built by the profile
      module_pass_manager.run(*module, analyser.moduleAnalysisManager());

      // Priniting either human readible LL code or byte
      // code as a result, depending on the users preference.
      if (config.emit_llvm)
      {
        llvm::errs() << *module << "\n";
      }
      else
      {
        llvm::errs() << "Byte code ouput is not supported yet. Please add -S to get human readible "
                        "LL code.\n";
      }

      // Verifying the module.
      if (config.verify_module)
      {
        llvm::VerifierAnalysis verifier;
        auto                   result = verifier.run(*module, analyser.moduleAnalysisManager());
        if (result.IRBroken)
        {
          llvm::errs() << "IR is broken."
                       << "\n";
          exit(-1);
        }
      }
    }

    if (config.validate)
    {
      // Creating pass builder
      LlvmAnalyser analyser{config.debug};

      // Creating a validation pass manager
      auto module_pass_manager =
          profile->createValidationModulePass(analyser.passBuilder(), optimisation_level, config.debug);
      module_pass_manager.run(*module, analyser.moduleAnalysisManager());
    }
  }
  catch (std::exception const &e)
  {
    llvm::errs() << "An error occured: " << e.what() << "\n";
  }

  return 0;
}
