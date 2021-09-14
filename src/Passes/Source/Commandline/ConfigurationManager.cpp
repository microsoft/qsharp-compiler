// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

using namespace microsoft::quantum;

namespace microsoft {
namespace quantum {

void ConfigurationManager::setupArguments(ParameterParser &parser)
{
  for (auto &section : config_sections_)
  {
    for (auto &c : section.settings)
    {
      c->setupArguments(parser);
    }
  }
}

void ConfigurationManager::configure(ParameterParser const &parser)
{
  for (auto &section : config_sections_)
  {
    for (auto &c : section.settings)
    {
      c->configure(parser);
    }
  }
}

void ConfigurationManager::printHelp() const
{
  std::cout << std::setfill(' ');
  for (auto &section : config_sections_)
  {
    std::cout << std::endl;
    std::cout << section.name << " - ";
    std::cout << section.description << std::endl;
    std::cout << std::endl;

    for (auto &c : section.settings)
    {
      if (c->isFlag())
      {
        if (c->defaultValue() == "false")
        {
          std::cout << std::setw(50) << std::left << ("--" + c->name()) << c->description() << " ";
        }
        else
        {
          std::cout << std::setw(50) << std::left << ("--[no-]" + c->name()) << c->description()
                    << " ";
        }
      }
      else
      {
        std::cout << std::setw(50) << std::left << ("--" + c->name()) << c->description() << " ";
      }
      std::cout << "Default: " << c->defaultValue() << std::endl;
    }
  }
}

void ConfigurationManager::printConfiguration() const
{
  std::cout << std::setfill('.');
  for (auto &section : config_sections_)
  {
    std::cout << "; # " << section.name << "\n";
    for (auto &c : section.settings)
    {
      std::cout << "; " << std::setw(50) << std::left << c->name() << ": " << c->value() << "\n";
    }
    std::cout << "; \n";
  }
}

void ConfigurationManager::setSectionName(String const &name, String const &description)
{
  config_sections_.back().name        = name;
  config_sections_.back().description = description;
}

}  // namespace quantum
}  // namespace microsoft
