#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

class ResourceRemapperPass : public llvm::PassInfoMixin<ResourceRemapperPass>
{
public:
  /// Constructors and destructors
  /// @{
  ResourceRemapperPass()                             = default;
  ResourceRemapperPass(ResourceRemapperPass const &) = default;
  ResourceRemapperPass(ResourceRemapperPass &&)      = default;
  ~ResourceRemapperPass()                            = default;
  /// @}

  /// Operators
  /// @{
  ResourceRemapperPass &operator=(ResourceRemapperPass const &) = default;
  ResourceRemapperPass &operator=(ResourceRemapperPass &&) = default;
  /// @}

  /// Functions required by LLVM
  /// @{
  llvm::PreservedAnalyses run(llvm::Function &function, llvm::FunctionAnalysisManager &fam);
  static bool             isRequired();
  /// @}
};

}  // namespace quantum
}  // namespace microsoft
