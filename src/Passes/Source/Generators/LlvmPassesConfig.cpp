// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Generators/LlvmPassesConfig.hpp"

namespace microsoft
{
namespace quantum
{

    void LlvmPassesConfiguration::setup(ConfigurationManager& config)
    {
        config.setSectionName("LLVM Passes", "Configuration of LLVM passes.");
        config.addParameter(always_inline_, "always-inline", "Aggresively inline function calls.");
    }

    LlvmPassesConfiguration LlvmPassesConfiguration::disable()
    {
        LlvmPassesConfiguration ret;
        ret.always_inline_ = false;
        return ret;
    }

    bool LlvmPassesConfiguration::alwaysInline() const
    {
        return always_inline_;
    }

    bool LlvmPassesConfiguration::isDisabled() const
    {
        return always_inline_ == false;
    }

    bool LlvmPassesConfiguration::isDefault() const
    {
        LlvmPassesConfiguration ref{};
        return always_inline_ == ref.always_inline_;
    }

} // namespace quantum
} // namespace microsoft
