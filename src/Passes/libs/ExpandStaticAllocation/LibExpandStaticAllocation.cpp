// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

#include "ExpandStaticAllocation/ExpandStaticAllocation.hpp"

#include <fstream>
#include <iostream>

namespace
{
llvm::PassPluginLibraryInfo getExpandStaticAllocationPluginInfo()
{
    using namespace microsoft::quantum;
    using namespace llvm;

    return {
        LLVM_PLUGIN_API_VERSION, "ExpandStaticAllocation", LLVM_VERSION_STRING,
        [](PassBuilder& pb)
        {
            // Registering the pass
            pb.registerPipelineParsingCallback(
                [](StringRef name, FunctionPassManager& fpm, ArrayRef<PassBuilder::PipelineElement> /*unused*/)
                {
                    if (name == "expand-static-allocation")
                    {
                        fpm.addPass(ExpandStaticAllocationPass());
                        return true;
                    }

                    return false;
                });
        }};
}
} // namespace

// Interface for loading the plugin
extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
    return getExpandStaticAllocationPluginInfo();
}
