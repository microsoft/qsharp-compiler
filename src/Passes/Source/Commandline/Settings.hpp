#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <iomanip>
#include <iostream>
#include <string>
#include <unordered_map>

namespace microsoft
{
namespace quantum
{

    class Settings
    {
      public:
        using String = std::string;

        using SettingsMap = std::unordered_map<String, String>;
        explicit Settings(SettingsMap default_settings);

        String get(String const& name, String const& default_value);
        String get(String const& name);

        /// Helper functions
        /// @{

        /// Prints
        void print();
        /// @}

        String& operator[](String const& key);

      private:
        SettingsMap settings_;
        friend class ParameterParser;
    };

} // namespace quantum
} // namespace microsoft
