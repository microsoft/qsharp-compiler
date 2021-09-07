#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft
{
namespace quantum
{

    struct LlvmPassesConfiguration
    {
        void setup(ConfigurationManager& config)
        {
            config.setSectionName("LLVM Passes", "Configuration of LLVM passes.");
            config.addParameter(always_inline, "always-inline", "Aggresively inline function calls.");
        }

        static LlvmPassesConfiguration disable()
        {
            LlvmPassesConfiguration ret;
            ret.always_inline = false;
            return ret;
        }

        bool always_inline{false};
    };

} // namespace quantum
} // namespace microsoft
