#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Logging/ILogger.hpp"

#include <vector>

namespace microsoft {
namespace quantum {

class LogCollection : public ILogger
{
public:
  struct Location
  {
    String   name{};
    uint64_t row{0};
    uint64_t col{0};
  };
  enum class Type
  {
    Debug,
    Info,
    Warning,
    Error,
    InternalError,
  };

  struct Message
  {
    Type     type;
    Location location;
    String   message;
  };
  using Messages = std::vector<Message>;

  void debug(String const &message) override
  {
    messages_.emplace_back({Debug, current_location_, message});
  }

  void info(String const &message) override
  {
    messages_.emplace_back({Info, current_location_, message});
  }

  void warning(String const &message) override
  {
    messages_.emplace_back({Warning, current_location_, message});
  }

  void error(String const &message) override
  {
    messages_.emplace_back({Error, current_location_, message});
  }

  void internalError(String const &message) override
  {
    messages_.emplace_back({InternalError, current_location_, message});
  }

  void setLocation(String const &name, uint64_t row, uint64_t col) override
  {
    current_location_.name = name;
    current_location_.row  = row;
    current_location_.col  = col;
  }

private:
  Location current_location_{};
  Messages messages_;
};

}  // namespace quantum
}  // namespace microsoft
