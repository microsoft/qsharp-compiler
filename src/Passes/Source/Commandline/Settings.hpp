#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <iomanip>
#include <iostream>
#include <string>
#include <unordered_map>

namespace microsoft {
namespace quantum {

class Settings
{
public:
  using String = std::string;

  using SettingsMap = std::unordered_map<String, String>;
  explicit Settings(SettingsMap default_settings);

  bool addSetting(String const &name, String value);

  /// Accessing settings
  /// @{
  /// Gets a named setting, falling back to a default if the key is not found.
  String get(String const &name, String const &default_value) noexcept;

  /// Gets a named setting. This method throws if the setting is not present.
  String get(String const &name);

  /// Access operator which forwards access to the underlying map.
  String &operator[](String const &key);
  /// @}

  /// Helper functions
  /// @{
  /// Prints the settings and their current values.
  void print();
  /// @}

private:
  SettingsMap settings_;  ///< Settings map that keeps all specified settings.
  friend class ParameterParser;
};

}  // namespace quantum
}  // namespace microsoft
