#include "Commandline/ParameterParser.hpp"
#include "Commandline/Settings.hpp"
#include "Llvm/Llvm.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Profiles/IProfile.hpp"
#include "Rules/Factory.hpp"

#include <iomanip>
#include <iostream>
#include <unordered_map>

using namespace llvm;
using namespace microsoft::quantum;

class BaseProfile : public IProfile
{
public:
  llvm::ModulePassManager createGenerationModulePass(
      llvm::PassBuilder &                   pass_builder,
      llvm::PassBuilder::OptimizationLevel &optimisation_level) override;
};

llvm::ModulePassManager BaseProfile::createGenerationModulePass(
    llvm::PassBuilder &pass_builder, llvm::PassBuilder::OptimizationLevel &optimisation_level)
{
  auto functionPassManager = pass_builder.buildFunctionSimplificationPipeline(
      optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, true);

  RuleSet rule_set;

  // Defining the mapping
  auto factory = RuleFactory(rule_set);

  factory.useStaticQuantumArrayAllocation();
  factory.useStaticQuantumAllocation();
  factory.useStaticResultAllocation();

  factory.optimiseBranchQuatumOne();
  //  factory.optimiseBranchQuatumZero();

  factory.disableReferenceCounting();
  factory.disableAliasCounting();
  factory.disableStringSupport();

  functionPassManager.addPass(TransformationRulePass(std::move(rule_set)));

  // https://llvm.org/docs/NewPassManager.html
  // modulePassManager.addPass(createModuleToCGSCCPassAdaptor(...));
  // InlinerPass()

  return pass_builder.buildPerModuleDefaultPipeline(llvm::PassBuilder::OptimizationLevel::O1);
}

int main(int argc, char **argv)
{
  Settings settings{{
      {"debug", "false"},
      {"profile", "qir"},
  }};

  ParameterParser parser(settings);
  parser.addFlag("debug");
  parser.parseArgs(argc, argv);
  settings.print();

  if (parser.arguments().empty())
  {
    std::cerr << "usage: " << argv[0] << " [options] filename" << std::endl;
    exit(-1);
  }

  LLVMContext  context;
  SMDiagnostic error;
  auto         module = parseIRFile(parser.getArg(0), error, context);
  if (module)
  {
    bool        debug              = settings.get("debug") == "true";
    auto        optimisation_level = llvm::PassBuilder::OptimizationLevel::O1;
    BaseProfile profile;

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

    auto modulePassManager = profile.createGenerationModulePass(pass_builder, optimisation_level);
    modulePassManager.run(*module, moduleAnalysisManager);

    llvm::errs() << *module << "\n";
  }

  return 0;
}
