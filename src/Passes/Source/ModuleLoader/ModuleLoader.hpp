#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

class ModuleLoader
{
public:
  using Module       = llvm::Module;
  using Linker       = llvm::Linker;
  using String       = std::string;
  using SMDiagnostic = llvm::SMDiagnostic;

  ModuleLoader(Module *final_module)
    : final_module_{final_module}
    , linker_{*final_module}
  {}

  bool addModule(std::unique_ptr<Module> &&module, String const &filename = "unknown")
  {
    if (verifyModule(*module, &llvm::errs()))
    {
      llvm::errs() << filename << ": "
                   << "input module is broken!\n";
      return false;
    }

    return !linker_.linkInModule(std::move(module), Linker::Flags::None);
  }

  bool addIrFile(String const &filename)
  {

    SMDiagnostic err;

    std::unique_ptr<Module> module = parseIRFile(filename, err, final_module_->getContext());
    if (!module)
    {
      llvm::errs() << "Failed to load " << filename << "\n";
      return false;
    }

    return addModule(std::move(module), filename);
  }

private:
  Module *final_module_;
  Linker  linker_;
};

}  // namespace quantum
}  // namespace microsoft
