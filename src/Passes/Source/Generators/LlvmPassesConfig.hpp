#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft
{
namespace quantum
{

    class LlvmPassesConfiguration
    {
      public:
        void setup(ConfigurationManager& config)
        {
            config.setSectionName("LLVM Passes", "Configuration of LLVM passes.");
            config.addParameter(always_inline_, "always-inline", "Aggresively inline function calls.");
        }

        static LlvmPassesConfiguration disable()
        {
            LlvmPassesConfiguration ret;
            ret.always_inline_ = false;
            return ret;
        }

        bool alwaysInline() const
        {
            return always_inline_;
        }

      private:
        bool always_inline_{false};
    };

} // namespace quantum
} // namespace microsoft
