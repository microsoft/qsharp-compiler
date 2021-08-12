#pragma once

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

class IProfile
{
public:
  IProfile() = default;
  virtual ~IProfile();
  virtual llvm::ModulePassManager createGenerationModulePass(
      llvm::PassBuilder &                   pass_builder,
      llvm::PassBuilder::OptimizationLevel &optimisation_level) = 0;
};

}  // namespace quantum
}  // namespace microsoft
