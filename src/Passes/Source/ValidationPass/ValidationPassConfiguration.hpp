#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Types/Types.hpp"

namespace microsoft
{
namespace quantum
{

    class ValidationPassConfiguration
    {
      public:
        using Set = std::unordered_set<std::string>;
        // Setup and construction
        //

        /// Setup function that attached the configuration to the ConfigurationManager.
        void setup(ConfigurationManager& config)
        {
            config.setSectionName("Validation configuration", "");
            config.addParameter(
                allow_internal_calls_, "allow-internal-calls", "Whether or not internal calls are allowed.");
        }

        static ValidationPassConfiguration fromProfileName(String const& name)
        {
            auto ret = ValidationPassConfiguration();
            if (name == "generic")
            {
                ret.allow_internal_calls_     = true;
                ret.whitelist_external_calls_ = false;
                ret.whitelist_opcodes_        = false;
            }
            else if (name == "base")
            {
                ret.allow_internal_calls_     = false;
                ret.whitelist_external_calls_ = true;
                ret.whitelist_opcodes_        = true;
                ret.opcodes_                  = Set{"br", "call", "unreachable", "ret"};
                ret.external_calls_           = Set{"__quantum__qis__mz__body",    "__quantum__qis__read_result__body",
                                          "__quantum__qis__reset__body", "__quantum__qis__z__body",
                                          "__quantum__qis__s__adj",      "__quantum__qis__dumpregister__body",
                                          "__quantum__qis__y__body",     "__quantum__qis__x__body",
                                          "__quantum__qis__t__body",     "__quantum__qis__cz__body",
                                          "__quantum__qis__s__body",     "__quantum__qis__h__body",
                                          "__quantum__qis__cnot__body",  "__quantum__qis__sqrt__body"};
            }
            else
            {
                throw std::runtime_error("Invalid profile " + name);
            }
            return ret;
        }

        Set const& allowedOpcodes() const
        {
            return opcodes_;
        }

        Set const& allowedExternalCallNames() const
        {
            return external_calls_;
        }

        bool allowInternalCalls() const
        {
            return allow_internal_calls_;
        }

        bool whitelistOpcodes() const
        {
            return whitelist_opcodes_;
        }

        bool whitelistExternalCalls() const
        {
            return whitelist_external_calls_;
        }

      private:
        Set opcodes_{};
        Set external_calls_{};

        bool whitelist_opcodes_{true};
        bool whitelist_external_calls_{true};
        bool allow_internal_calls_{false};
    };

} // namespace quantum
} // namespace microsoft
