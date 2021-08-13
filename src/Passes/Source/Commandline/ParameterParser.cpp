// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ParameterParser.hpp"

#include <string>
#include <unordered_map>

namespace microsoft
{
namespace quantum
{

    ParameterParser::ParameterParser(Settings& settings)
      : settings_{settings}
    {
    }

    void ParameterParser::parseArgs(int argc, char** argv)
    {
        uint64_t                 i = 1;
        std::vector<ParsedValue> values;
        while (i < static_cast<uint64_t>(argc))
        {
            values.push_back(parseSingleArg(argv[i]));
            ++i;
        }

        i = 0;
        while (i < values.size())
        {
            auto& v = values[i];
            ++i;

            if (!v.is_key)
            {
                arguments_.push_back(v.value);
                continue;
            }

            if (i >= values.size())
            {
                settings_[v.value] = "true";
                continue;
            }

            auto& v2 = values[i];
            if (!v2.is_key && hasValue(v.value))
            {
                settings_[v.value] = v2.value;
                ++i;
                continue;
            }

            settings_[v.value] = "true";
        }
    }

    void ParameterParser::addFlag(String const& v)
    {
        flags_.insert(v);
    }

    ParameterParser::Arguments const& ParameterParser::arguments() const
    {
        return arguments_;
    }
    ParameterParser::String const& ParameterParser::getArg(uint64_t const& n)
    {
        return arguments_[n];
    }

    ParameterParser::ParsedValue ParameterParser::parseSingleArg(String key)
    {
        bool is_key = false;
        if (key.size() > 2 && key.substr(0, 2) == "--")
        {
            is_key = true;
            key    = key.substr(2);
        }
        else if (key.size() > 1 && key.substr(0, 1) == "-")
        {
            is_key = true;
            key    = key.substr(1);
        }
        return {is_key, key};
    }

    bool ParameterParser::hasValue(String const& key)
    {
        if (flags_.find(key) != flags_.end())
        {
            return false;
        }

        return true;
    }

} // namespace quantum
} // namespace microsoft
