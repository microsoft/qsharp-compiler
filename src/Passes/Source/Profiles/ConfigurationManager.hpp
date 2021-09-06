#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ParameterParser.hpp"
#include "Llvm/Llvm.hpp"

#include <iostream>
#include <typeindex>

namespace microsoft {
namespace quantum {

class IConfigBind
{
public:
  using String = std::string;

  IConfigBind(String const name, String const &description)
    : name_{name}
    , description_{description}
  {}
  virtual ~IConfigBind();

  virtual bool setupArguments(ParameterParser &parser)  = 0;
  virtual bool configure(ParameterParser const &parser) = 0;

  String name() const
  {
    return name_;
  }

  String description() const
  {
    return description_;
  }

private:
  String name_;
  String description_;
};

template <typename T>
class ConfigBind : public IConfigBind
{
public:
  using String = std::string;
  using Type   = T;
  ConfigBind(Type &bind, T default_value, String const name, String const &description)
    : IConfigBind(name, description)
    , bind_{bind}
    , default_value_{std::move(default_value)}
  {}

  bool setupArguments(ParameterParser &parser) override
  {
    parser.addFlag(name());
    return true;
  }

  bool configure(ParameterParser const & /*parser*/) override
  {
    bind_ = default_value_;
    return true;
  }

  Type &bind()
  {
    return bind_;
  }

private:
  Type &bind_;
  Type  default_value_;
};

struct ConfigurationManager
{
public:
  using String         = std::string;
  using IConfigBindPtr = std::shared_ptr<IConfigBind>;
  using ConfigList     = std::vector<IConfigBindPtr>;
  using VoidPtr        = std::shared_ptr<void>;
  struct Section
  {
    Section(std::type_index t)
      : type{t}
    {}
    String          name;
    String          description;
    VoidPtr         configuration;
    std::type_index type;
    ConfigList      settings;
  };
  using Sections = std::vector<Section>;

  ConfigurationManager()
  {
    /*
    // RuleSet / Factory settings
    addSection("Transformation rules", "Rules used to transform instruction sequences in the QIR.");
    addParameter(factory_configuration_.disable_reference_counting, "disable-reference-counting",
                 "Disables reference counting by instruction removal.");

    addParameter(factory_configuration_.disable_reference_counting, "disable-reference-counting",
                 "Disables reference counting by instruction removal.");
    addParameter(factory_configuration_.disable_alias_counting, "disable-alias-counting",
                 "Disables alias counting by instruction removal.");
    addParameter(factory_configuration_.disable_string_support, "disable-string-support",
                 "Disables string support by instruction removal.");
    addParameter(factory_configuration_.optimise_branch_quatum_one, "optimise-branch-quatum-one",
                 "Maps branching based on quantum measurements compared to one to base profile "
                 "type measurement.");
    addParameter(factory_configuration_.optimise_branch_quatum_zero, "optimise-branch-quatum-zero",
                 "Maps branching based on quantum measurements compared to zero to base profile "
                 "type measurement.");
    addParameter(factory_configuration_.use_static_qubit_array_allocation,
                 "use-static-qubit-array-allocation",
                 "Maps allocation of qubit arrays to static array allocation.");
    addParameter(factory_configuration_.use_static_qubit_allocation, "use-static-qubit-allocation",
                 "Maps qubit allocation to static allocation.");
    addParameter(factory_configuration_.use_static_result_allocation,
                 "use-static-result-allocation", "Maps result allocation to static allocation.");

    // Pass
    addSection("Pass configuration",
               "Configuration of the pass and its corresponding optimisations.");
    addParameter(pass_configuration_.always_inline, "always-inline",
                 "Aggresively inline function calls.");
    addParameter(pass_configuration_.delete_dead_code, "delete-dead-code", "Deleted dead code");
    addParameter(pass_configuration_.max_recursion, "max-recursion", "max-recursion");
    addParameter(pass_configuration_.reuse_qubits, "reuse-qubits", "reuse-qubits");
    addParameter(pass_configuration_.group_measurements, "group-measurements",
                 "NOT IMPLEMENTED - group-measurements");
    addParameter(pass_configuration_.one_shot_measurement, "one-shot-measurement",
                 "NOT IMPLEMENTED - one-shot-measurement");
    /// @}
    */
  }

  ConfigurationManager(ConfigurationManager const &) = delete;
  ConfigurationManager(ConfigurationManager &&)      = delete;
  ConfigurationManager &operator=(ConfigurationManager const &) = delete;
  ConfigurationManager &operator=(ConfigurationManager &&) = delete;

  void setupArguments(ParameterParser &parser)
  {
    for (auto &section : config_sections_)
    {
      for (auto &c : section.settings)
      {
        c->setupArguments(parser);
      }
    }
  }

  void configure(ParameterParser const &parser)
  {
    for (auto &section : config_sections_)
    {
      for (auto &c : section.settings)
      {
        c->configure(parser);
      }
    }
  }

  void printHelp() const
  {
    for (auto &section : config_sections_)
    {
      std::cout << std::endl;
      std::cout << section.name << " - ";
      std::cout << section.description << std::endl;
      std::cout << std::endl;

      for (auto &c : section.settings)
      {
        std::cout << std::setw(50) << std::left << ("--" + c->name()) << c->description() << " ("
                  << ")" << std::endl;
      }
    }
  }

  template <typename T>
  void addConfig()
  {
    Section new_section{std::type_index(typeid(T))};

    auto ptr                  = std::make_shared<T>();
    new_section.configuration = ptr;

    config_sections_.emplace_back(std::move(new_section));
    ptr->setup(*this);
  }

  void setSectionName(String const &name, String const &description)
  {
    config_sections_.back().name        = name;
    config_sections_.back().description = description;
  }

  template <typename T>
  void addParameter(T &bind, T default_value, String const &name, String const &description)
  {
    auto ptr = std::make_shared<ConfigBind<T>>(bind, default_value, name, description);
    config_sections_.back().settings.push_back(ptr);
  }

  template <typename T>
  void addParameter(T &bind, String const &name, String const &description)
  {
    auto ptr = std::make_shared<ConfigBind<T>>(bind, T(bind), name, description);
    config_sections_.back().settings.push_back(ptr);
  }

private:
  Sections config_sections_;
};

struct FactoryConfiguration
{
  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Transformation rules",
                          "Rules used to transform instruction sequences in the QIR.");
    config.addParameter(disable_reference_counting, "disable-reference-counting",
                        "Disables reference counting by instruction removal.");

    config.addParameter(disable_reference_counting, "disable-reference-counting",
                        "Disables reference counting by instruction removal.");
    config.addParameter(disable_alias_counting, "disable-alias-counting",
                        "Disables alias counting by instruction removal.");
    config.addParameter(disable_string_support, "disable-string-support",
                        "Disables string support by instruction removal.");
    config.addParameter(
        optimise_branch_quatum_one, "optimise-branch-quatum-one",
        "Maps branching based on quantum measurements compared to one to base profile "
        "type measurement.");
    config.addParameter(
        optimise_branch_quatum_zero, "optimise-branch-quatum-zero",
        "Maps branching based on quantum measurements compared to zero to base profile "
        "type measurement.");
    config.addParameter(use_static_qubit_array_allocation, "use-static-qubit-array-allocation",
                        "Maps allocation of qubit arrays to static array allocation.");
    config.addParameter(use_static_qubit_allocation, "use-static-qubit-allocation",
                        "Maps qubit allocation to static allocation.");
    config.addParameter(use_static_result_allocation, "use-static-result-allocation",
                        "Maps result allocation to static allocation.");
  }

  /// Factory Configuration
  /// @{
  bool disable_reference_counting{true};
  bool disable_alias_counting{true};
  bool disable_string_support{true};
  /// @}

  /// Optimisations
  /// @{
  bool optimise_branch_quatum_one{true};
  bool optimise_branch_quatum_zero{true};
  /// @}

  bool use_static_qubit_array_allocation{true};
  bool use_static_qubit_allocation{true};
  bool use_static_result_allocation{true};
};

struct PassConfiguration
{
  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Pass configuration",
                          "Configuration of the pass and its corresponding optimisations.");
    config.addParameter(always_inline, "always-inline", "Aggresively inline function calls.");
    config.addParameter(delete_dead_code, "delete-dead-code", "Deleted dead code");
    config.addParameter(max_recursion, "max-recursion", "max-recursion");
    config.addParameter(reuse_qubits, "reuse-qubits", "reuse-qubits");
    config.addParameter(group_measurements, "group-measurements",
                        "NOT IMPLEMENTED - group-measurements");
    config.addParameter(one_shot_measurement, "one-shot-measurement",
                        "NOT IMPLEMENTED - one-shot-measurement");
  }
  bool always_inline{false};
  bool delete_dead_code{true};

  /// Const-expression
  /// @{
  int32_t max_recursion{512};
  /// @}

  /// Allocation options
  /// @{
  bool reuse_qubits{true};  // NOT IMPLEMENTED
  /// @}

  /// Measurement
  /// @{
  bool group_measurements{false};   // NOT IMPLEMENTED
  bool one_shot_measurement{true};  // NOT IMPLEMENTED
  /// @}
};

}  // namespace quantum
}  // namespace microsoft
