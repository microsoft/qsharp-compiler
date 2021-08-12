#pragma once
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

  struct ParsedValue
  {
    bool   is_key{false};
    String value;
  };

  ParameterParser(Settings &settings);

  void             parseArgs(int argc, char **argv);
  void             addFlag(String const &v);
  Arguments const &arguments() const;
  String const &   getArg(uint64_t const &n);

private:
  ParsedValue parseSingleArg(String key);

  bool hasValue(String const &key);

  Settings &settings_;
  Arguments arguments_{};
  Flags     flags_{};
};

}  // namespace quantum
}  // namespace microsoft
