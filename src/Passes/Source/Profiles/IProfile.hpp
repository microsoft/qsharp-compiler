#pragma once

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

class IProfile
{
public:
  using PassBuilder             = llvm::PassBuilder;
  using OptimizationLevel       = PassBuilder::OptimizationLevel;
  using FunctionAnalysisManager = llvm::FunctionAnalysisManager;

  IProfile() = default;
  virtual ~IProfile();
  virtual llvm::ModulePassManager createGenerationModulePass(PassBuilder &      pass_builder,
                                                             OptimizationLevel &optimisation_level,
                                                             bool               debug)            = 0;
  virtual llvm::ModulePassManager createValidationModulePass(PassBuilder &      pass_builder,
                                                             OptimizationLevel &optimisation_level,
                                                             bool               debug)            = 0;
  virtual void                    addFunctionAnalyses(FunctionAnalysisManager &fam) = 0;
};

}  // namespace quantum
}  // namespace microsoft
