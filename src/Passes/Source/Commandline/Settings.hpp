#pragma once

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
  Settings(SettingsMap default_settings)
    : settings_{default_settings}
  {}

  String get(String const &name, String const &default_value)
  {
    auto it = settings_.find(name);
    if (it == settings_.end())
    {
      return default_value;
    }

    return it->second;
  }

  String get(String const &name)
  {
    auto it = settings_.find(name);
    if (it == settings_.end())
    {
      throw std::runtime_error("Could not find setting '" + name + "'.");
    }

    return it->second;
  }

  void print()
  {
    std::cout << "Settings" << std::endl;
    for (auto &s : settings_)
    {
      std::cout << std::setw(20) << s.first << ": " << s.second << std::endl;
    }
  }

  String &operator[](String const &key)
  {
    return settings_[key];
  }

private:
  SettingsMap settings_;
  friend class ParameterParser;
};

}  // namespace quantum
}  // namespace microsoft
