// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/Settings.hpp"

#include <iomanip>
#include <iostream>
#include <string>
#include <unordered_map>

namespace microsoft
{
namespace quantum
{

    Settings::Settings(SettingsMap default_settings)
      : settings_{std::move(default_settings)}
    {
    }

    Settings::String Settings::get(String const& name, String const& default_value)
    {
        auto it = settings_.find(name);
        if (it == settings_.end())
        {
            return default_value;
        }

        return it->second;
    }

    Settings::String Settings::get(String const& name)
    {
        auto it = settings_.find(name);
        if (it == settings_.end())
        {
            throw std::runtime_error("Could not find setting '" + name + "'.");
        }

        return it->second;
    }

    void Settings::print()
    {
        std::cout << "Settings" << std::endl;
        for (auto& s : settings_)
        {
            std::cout << std::setw(20) << s.first << ": " << s.second << std::endl;
        }
    }

    Settings::String& Settings::operator[](String const& key)
    {
        return settings_[key];
    }

} // namespace quantum
} // namespace microsoft
