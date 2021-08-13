#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/OperandPrototype.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

class CallPattern : public OperandPrototype
{
public:
  using String = std::string;
  CallPattern(String const &name);

  ~CallPattern() override;

  bool  match(Value *instr, Captures &captures) const override;
  Child copy() const override;

private:
  String name_{};
};

}  // namespace quantum
}  // namespace microsoft
