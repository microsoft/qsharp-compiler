// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Apps/Qat/LlvmAnalysis.hpp"
#include "Commandline/ParameterParser.hpp"
#include "Commandline/Settings.hpp"
#include "Profiles/BaseProfile.hpp"
#include "Profiles/IProfile.hpp"

#include "Llvm/Llvm.hpp"

#include <iomanip>
#include <iostream>
#include <unordered_map>

using namespace llvm;
using namespace microsoft::quantum;

int main(int argc, char** argv)
{
    try
    {
        // Parsing commmandline arguments
        Settings settings{
            {{"debug", "false"},
             {"generate", "false"},
             {"validate", "false"},
             {"profile", "base-profile"},
             {"S", "false"}}};

        ParameterParser parser(settings);
        parser.addFlag("debug");
        parser.addFlag("generate");
        parser.addFlag("validate");
        parser.addFlag("S");

        parser.parseArgs(argc, argv);

        if (parser.arguments().empty())
        {
            std::cerr << "Usage: " << argv[0] << " [options] filename" << std::endl;
            exit(-1);
        }

        // Loading IR
        LLVMContext  context;
        SMDiagnostic error;
        auto         module = parseIRFile(parser.getArg(0), error, context);

        if (!module)
        {
            std::cerr << "Invalid IR." << std::endl;
            exit(-1);
        }

        // Extracting commandline parameters
        bool                         debug              = settings.get("debug") == "true";
        bool                         generate           = settings.get("generate") == "true";
        bool                         validate           = settings.get("validate") == "true";
        auto                         optimisation_level = llvm::PassBuilder::OptimizationLevel::O1;
        std::shared_ptr<BaseProfile> profile            = std::make_shared<BaseProfile>();

        // In case we debug, we also print the settings to allow provide a full
        // picture of what is going.
        if (debug)
        {
            settings.print();
        }

        // Checking if we are asked to generate a new QIR. If so, we will use
        // the profile to setup passes to
        if (generate)
        {
            // Creating pass builder
            LlvmAnalyser analyser{debug};

            // Preparing pass for generation based on profile
            profile->addFunctionAnalyses(analyser.functionAnalysisManager());
            auto module_pass_manager =
                profile->createGenerationModulePass(analyser.passBuilder(), optimisation_level, debug);

            // Running the pass built by the profile
            module_pass_manager.run(*module, analyser.moduleAnalysisManager());

            // Priniting either human readible LL code or byte
            // code as a result, depending on the users preference.
            if (settings.get("S") == "true")
            {
                llvm::errs() << *module << "\n";
            }
            else
            {
                llvm::errs() << "Byte code ouput is not supported yet. Please add -S to get human readible "
                                "LL code.\n";
            }
        }

        if (validate)
        {
            // Creating pass builder
            LlvmAnalyser analyser{debug};

            // Creating a validation pass manager
            auto module_pass_manager =
                profile->createValidationModulePass(analyser.passBuilder(), optimisation_level, debug);
            module_pass_manager.run(*module, analyser.moduleAnalysisManager());
        }
    }
    catch (std::exception const& e)
    {
        llvm::errs() << "An error occured: " << e.what() << "\n";
    }

    return 0;
}
