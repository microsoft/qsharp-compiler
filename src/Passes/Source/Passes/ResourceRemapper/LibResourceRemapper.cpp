// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "ResourceRemapper/ResourceRemapper.hpp"

#include "Llvm/Llvm.hpp"

#include <fstream>
#include <iostream>

namespace
{
llvm::PassPluginLibraryInfo getResourceRemapperPluginInfo()
{
    using namespace microsoft::quantum;
    using namespace llvm;

    return {LLVM_PLUGIN_API_VERSION, "ResourceRemapper", LLVM_VERSION_STRING, [](PassBuilder& pb) {
                // Registering the pass
                pb.registerPipelineParsingCallback(
                    [](StringRef name, FunctionPassManager& fpm, ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
                        if (name == "resource-remapper")
                        {
                            fpm.addPass(ResourceRemapperPass());
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
    return getResourceRemapperPluginInfo();
}
