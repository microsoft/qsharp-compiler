#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/Settings.hpp"

#include <string>
#include <unordered_map>
#include <unordered_set>
#include <vector>

namespace microsoft {
namespace quantum {

class ParameterParser
{
public:
  using String    = std::string;
  using Arguments = std::vector<String>;
  using Flags     = std::unordered_set<String>;

  /// Construction and deconstrution configuration
  /// @{
  /// Parameter parsers requires a setting class to store
  /// parameters passed.
  explicit ParameterParser(Settings &settings);

  // Allow move semantics only. No default construction
  ParameterParser()                             = delete;
  ParameterParser(ParameterParser const &other) = delete;
  ParameterParser(ParameterParser &&other)      = default;
  ~ParameterParser()                            = default;
  /// @}

  /// Configuration
  /// @{

  /// Marks a name as a flag (as opposed to an option).
  /// This ensures that no parameter is expected after
  /// the flag is specified. For instance `--debug` is
  /// a flag as opposed to `--log-level 3` which is an
  /// option.
  void addFlag(String const &v);
  /// @}

  /// Operation
  /// @{
  /// Parses the command line arguments given the argc and argv
  /// from the main function.
  void parseArgs(int argc, char **argv);

  /// Returns list of arguments without flags and/or options
  /// included.
  Arguments const &arguments() const;
  String const &   getArg(uint64_t const &n);
  /// @}
private:
  struct ParsedValue
  {
    bool   is_key{false};
    String value;
  };

  /// Helper functions and variables
  /// @{
  ParsedValue parseSingleArg(String key);
  bool        hasValue(String const &key);
  Flags       flags_{};
  /// @}

  /// Storage of parsed data
  /// @{
  Settings &settings_;
  Arguments arguments_{};
  /// @}
};

}  // namespace quantum
}  // namespace microsoft
