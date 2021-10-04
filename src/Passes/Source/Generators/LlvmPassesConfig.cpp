// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Generators/LlvmPassesConfig.hpp"

namespace microsoft
{
namespace quantum
{

    LlvmPassesConfiguration::LlvmPassesConfiguration() = default;

    void LlvmPassesConfiguration::setup(ConfigurationManager& config)
    {
        config.setSectionName("LLVM Passes", "Configuration of LLVM passes.");
        config.addParameter(always_inline_, "always-inline", "Aggressively inline function calls.");
        config.addParameter(pass_pipeline_, "passes", "LLVM passes pipeline to use upon applying this component.");
    }

    LlvmPassesConfiguration LlvmPassesConfiguration::disable()
    {
        LlvmPassesConfiguration ret;
        ret.always_inline_ = false;
        ret.pass_pipeline_ = "";
        return ret;
    }

    bool LlvmPassesConfiguration::alwaysInline() const
    {
        return always_inline_;
    }

    std::string LlvmPassesConfiguration::passPipeline() const
    {
        return pass_pipeline_;
    }

    bool LlvmPassesConfiguration::isDisabled() const
    {
        return always_inline_ == false && pass_pipeline_ == "";
    }

    bool LlvmPassesConfiguration::isDefault() const
    {
        LlvmPassesConfiguration ref{};
        return always_inline_ == ref.always_inline_;
    }

} // namespace quantum
} // namespace microsoft
