#include "Llvm.hpp"

using namespace llvm;
int main(int /*argc*/, char **argv)
{
  LLVMContext  context;
  SMDiagnostic error;
  auto         module = parseIRFile(argv[1], error, context);
  if (module)
  {
    ModulePassManager   MPM;
    FunctionPassManager FPM;

    // InstSimplifyPass is a function pass
    FPM.addPass(LoopSimplifyPass());
    MPM.addPass(createModuleToFunctionPassAdaptor(std::move(FPM)));

    //    MPM.run(*module);
    //    m->print(llvm)
    llvm::errs() << *module << "\n";
  }

  return 0;
}
