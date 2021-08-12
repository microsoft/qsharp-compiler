#pragma once
#include "Llvm/Llvm.hpp"
#include "Profiles/IProfile.hpp"

namespace microsoft {
namespace quantum {

class BaseProfile : public IProfile
{
public:
  llvm::ModulePassManager createGenerationModulePass(
      PassBuilder &pass_builder, OptimizationLevel &optimisation_level) override;
  llvm::ModulePassManager createValidationModulePass(
      PassBuilder &pass_builder, OptimizationLevel &optimisation_level) override;
};
}  // namespace quantum
}  // namespace microsoft
