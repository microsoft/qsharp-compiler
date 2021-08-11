#include "Llvm/Llvm.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Rules/Factory.hpp"

using namespace llvm;
using namespace microsoft::quantum;

int main(int /*argc*/, char **argv)
{
  LLVMContext  context;
  SMDiagnostic error;
  auto         module = parseIRFile(argv[1], error, context);
  if (module)
  {

    llvm::PassBuilder             passBuilder;
    llvm::LoopAnalysisManager     loopAnalysisManager(true);  // true is just to output debug info
    llvm::FunctionAnalysisManager functionAnalysisManager(true);
    llvm::CGSCCAnalysisManager    cGSCCAnalysisManager(true);
    llvm::ModuleAnalysisManager   moduleAnalysisManager(true);

    passBuilder.registerModuleAnalyses(moduleAnalysisManager);
    passBuilder.registerCGSCCAnalyses(cGSCCAnalysisManager);
    passBuilder.registerFunctionAnalyses(functionAnalysisManager);
    passBuilder.registerLoopAnalyses(loopAnalysisManager);
    // This is the important line:
    passBuilder.crossRegisterProxies(loopAnalysisManager, functionAnalysisManager,
                                     cGSCCAnalysisManager, moduleAnalysisManager);

    auto functionPassManager = passBuilder.buildFunctionSimplificationPipeline(
        llvm::PassBuilder::OptimizationLevel::O1, llvm::PassBuilder::ThinLTOPhase::None, true);

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

    llvm::ModulePassManager modulePassManager =
        passBuilder.buildPerModuleDefaultPipeline(llvm::PassBuilder::OptimizationLevel::O1);

    // https://llvm.org/docs/NewPassManager.html
    // modulePassManager.addPass(createModuleToCGSCCPassAdaptor(...));
    // InlinerPass()

    modulePassManager.run(*module, moduleAnalysisManager);

    llvm::errs() << *module << "\n";
  }

  return 0;
}
