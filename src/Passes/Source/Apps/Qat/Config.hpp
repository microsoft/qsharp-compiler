#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft
{
namespace quantum
{
    class QatConfig
    {
      public:
        using String = std::string;

        void   setup(ConfigurationManager& config);
        bool   generate() const;
        bool   validate() const;
        String profile() const;
        bool   emitLlvm() const;
        bool   opt0() const;
        bool   opt1() const;
        bool   opt2() const;
        bool   opt3() const;
        bool   verifyModule() const;
        bool   debug() const;
        bool   dumpConfig() const;

      private:
        bool   generate_{false};
        bool   validate_{false};
        String profile_{"baseProfile"};
        bool   emit_llvm_{false};
        bool   opt0_{false};
        bool   opt1_{false};
        bool   opt2_{false};
        bool   opt3_{false};
        bool   verify_module_{false};

        bool debug_{false};
        bool dump_config_{false};
    };
} // namespace quantum
} // namespace microsoft
