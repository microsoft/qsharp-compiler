#include "Commandline/ParameterParser.hpp"
#include "Commandline/Settings.hpp"
#include "Llvm/Llvm.hpp"
#include "Profiles/BaseProfile.hpp"
#include "Profiles/IProfile.hpp"

#include <iomanip>
#include <iostream>
#include <unordered_map>

using namespace llvm;
using namespace microsoft::quantum;

int main(int argc, char **argv)
{
  // Parsing commmandline arguments
  Settings settings{{
      {"debug", "false"},
      {"generate", "false"},
      {"validate", "false"},
      {"profile", "base-profile"},
  }};

  ParameterParser parser(settings);
  parser.addFlag("debug");
  parser.addFlag("generate");
  parser.addFlag("validate");

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

  // settings.print();

  // Generating IR
  bool        debug              = settings.get("debug") == "true";
  bool        generate           = settings.get("generate") == "true";
  bool        validate           = settings.get("validate") == "true";
  auto        optimisation_level = llvm::PassBuilder::OptimizationLevel::O1;
  BaseProfile profile;

  // Worth looking at:
  // https://opensource.apple.com/source/lldb/lldb-76/llvm/tools/opt/opt.cpp

  if (generate)
  {
    // Creating pass builder
    llvm::PassBuilder             pass_builder;
    llvm::LoopAnalysisManager     loopAnalysisManager(debug);
    llvm::FunctionAnalysisManager functionAnalysisManager(debug);
    llvm::CGSCCAnalysisManager    cGSCCAnalysisManager(debug);
    llvm::ModuleAnalysisManager   moduleAnalysisManager(debug);

    pass_builder.registerModuleAnalyses(moduleAnalysisManager);
    pass_builder.registerCGSCCAnalyses(cGSCCAnalysisManager);
    pass_builder.registerFunctionAnalyses(functionAnalysisManager);
    pass_builder.registerLoopAnalyses(loopAnalysisManager);

    pass_builder.crossRegisterProxies(loopAnalysisManager, functionAnalysisManager,
                                      cGSCCAnalysisManager, moduleAnalysisManager);

    profile.addFunctionAnalyses(functionAnalysisManager);
    auto modulePassManager =
        profile.createGenerationModulePass(pass_builder, optimisation_level, debug);

    modulePassManager.run(*module, moduleAnalysisManager);

    //

    llvm::legacy::PassManager legacy_pass_manager;
    legacy_pass_manager.add(llvm::createCalledValuePropagationPass());
    legacy_pass_manager.add(llvm::createCalledValuePropagationPass());
    legacy_pass_manager.add(llvm::createConstantMergePass());
    legacy_pass_manager.run(*module);

    llvm::errs() << *module << "\n";
  }

  if (validate)
  {
    // Creating pass builder
    llvm::PassBuilder             pass_builder;
    llvm::LoopAnalysisManager     loopAnalysisManager(debug);
    llvm::FunctionAnalysisManager functionAnalysisManager(debug);
    llvm::CGSCCAnalysisManager    cGSCCAnalysisManager(debug);
    llvm::ModuleAnalysisManager   moduleAnalysisManager(debug);

    pass_builder.registerModuleAnalyses(moduleAnalysisManager);
    pass_builder.registerCGSCCAnalyses(cGSCCAnalysisManager);
    pass_builder.registerFunctionAnalyses(functionAnalysisManager);
    pass_builder.registerLoopAnalyses(loopAnalysisManager);

    pass_builder.crossRegisterProxies(loopAnalysisManager, functionAnalysisManager,
                                      cGSCCAnalysisManager, moduleAnalysisManager);

    auto modulePassManager =
        profile.createValidationModulePass(pass_builder, optimisation_level, debug);
    modulePassManager.run(*module, moduleAnalysisManager);
  }

  return 0;
}
