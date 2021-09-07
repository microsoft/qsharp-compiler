#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <cstdint>
#include <string>

namespace microsoft {
namespace quantum {

class ILogger
{
public:
  using String                                                             = std::string;
  virtual ~ILogger()                                                       = default;
  virtual void debug(String const &message)                                = 0;
  virtual void info(String const &message)                                 = 0;
  virtual void warning(String const &message)                              = 0;
  virtual void error(String const &message)                                = 0;
  virtual void internalError(String const &message)                        = 0;
  virtual void setLocation(String const &name, uint64_t row, uint64_t col) = 0;

private:
};

}  // namespace quantum
}  // namespace microsoft
