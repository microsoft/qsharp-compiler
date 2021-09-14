#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigBind.hpp"
#include "Commandline/ConfigurationBindInterface.hpp"
#include "Commandline/ParameterParser.hpp"
#include "Llvm/Llvm.hpp"

#include <iomanip>
#include <iostream>
#include <sstream>
#include <type_traits>
#include <typeindex>

namespace microsoft {
namespace quantum {

struct ConfigurationManager
{
public:
  using String         = std::string;
  using IConfigBindPtr = std::shared_ptr<IConfigBind>;
  using ConfigList     = std::vector<IConfigBindPtr>;
  using VoidPtr        = std::shared_ptr<void>;
  struct Section
  {
    std::type_index type{std::type_index(typeid(nullptr_t))};
    String          name{};
    String          description{};
    VoidPtr         configuration{};

    ConfigList settings{};
  };
  using Sections = std::vector<Section>;

  ConfigurationManager()                             = default;
  ConfigurationManager(ConfigurationManager const &) = delete;
  ConfigurationManager(ConfigurationManager &&)      = delete;
  ConfigurationManager &operator=(ConfigurationManager const &) = delete;
  ConfigurationManager &operator=(ConfigurationManager &&) = delete;

  void setupArguments(ParameterParser &parser);
  void configure(ParameterParser const &parser);
  void printHelp() const;
  void printConfiguration() const;

  void setSectionName(String const &name, String const &description);

  template <typename T>
  inline void addConfig();
  template <typename T>
  inline void setConfig(T const &value);
  template <typename T>
  inline T const &get() const;
  template <typename T>
  inline void addParameter(T &bind, T default_value, String const &name, String const &description);
  template <typename T>
  inline void addParameter(T &bind, String const &name, String const &description);

private:
  Sections config_sections_;
};

template <typename T>
inline void ConfigurationManager::addConfig()
{
  Section new_section{std::type_index(typeid(T))};

  auto ptr                  = std::make_shared<T>();
  new_section.configuration = ptr;

  config_sections_.emplace_back(std::move(new_section));
  ptr->setup(*this);
}

template <typename T>
inline void ConfigurationManager::setConfig(T const &value)
{
  auto    type = std::type_index(typeid(T));
  VoidPtr ptr{nullptr};
  for (auto &section : config_sections_)
  {
    if (section.type == type)
    {
      ptr = section.configuration;
      break;
    }
  }

  if (ptr == nullptr)
  {
    throw std::runtime_error("Could not find configuration class.");
  }

  auto &config = *static_cast<T *>(ptr.get());
  config       = value;
}

template <typename T>
inline T const &ConfigurationManager::get() const
{
  VoidPtr ptr{nullptr};
  auto    type = std::type_index(typeid(T));

  for (auto &section : config_sections_)
  {
    if (section.type == type)
    {
      ptr = section.configuration;
      break;
    }
  }

  if (ptr == nullptr)
  {
    throw std::runtime_error("Could not find configuration class.");
  }

  return *static_cast<T *>(ptr.get());
}

template <typename T>
inline void ConfigurationManager::addParameter(T &bind, T default_value, String const &name,
                                               String const &description)
{
  auto ptr = std::make_shared<ConfigBind<T>>(bind, default_value, name, description);
  config_sections_.back().settings.push_back(ptr);
}

template <typename T>
inline void ConfigurationManager::addParameter(T &bind, String const &name,
                                               String const &description)
{
  auto ptr = std::make_shared<ConfigBind<T>>(bind, T(bind), name, description);
  config_sections_.back().settings.push_back(ptr);
}
}  // namespace quantum
}  // namespace microsoft
