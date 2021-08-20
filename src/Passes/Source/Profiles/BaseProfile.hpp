#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Profiles/IProfile.hpp"

namespace microsoft {
namespace quantum {

class BaseProfile : public IProfile
{
public:
  llvm::ModulePassManager createGenerationModulePass(PassBuilder &            pass_builder,
                                                     OptimizationLevel const &optimisation_level,
                                                     bool                     debug) override;
  llvm::ModulePassManager createValidationModulePass(PassBuilder &            pass_builder,
                                                     OptimizationLevel const &optimisation_level,
                                                     bool                     debug) override;
  void                    addFunctionAnalyses(FunctionAnalysisManager &fam) override;
};

}  // namespace quantum
}  // namespace microsoft
