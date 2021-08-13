// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "ResourceRemapper/ResourceRemapper.hpp"

#include "Llvm/Llvm.hpp"

#include <fstream>
#include <iostream>

namespace microsoft
{
namespace quantum
{
    llvm::PreservedAnalyses ResourceRemapperPass::run(llvm::Function& function, llvm::FunctionAnalysisManager& /*fam*/)
    {
        // Pass body

        llvm::errs() << "Implement your pass here: " << function.getName() << "\n";

        return llvm::PreservedAnalyses::all();
    }

    bool ResourceRemapperPass::isRequired()
    {
        return true;
    }

} // namespace quantum
} // namespace microsoft
