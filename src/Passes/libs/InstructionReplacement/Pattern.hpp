#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

class OperandPrototype
{
public:
  using Instruction = llvm::Instruction;
  using String      = std::string;
  using Value       = llvm::Value;
  using Child       = std::shared_ptr<OperandPrototype>;
  using Children    = std::vector<Child>;
  using Captures    = std::unordered_map<std::string, Value *>;

  OperandPrototype() = default;

  virtual ~OperandPrototype();
  virtual bool match(Value *value, Captures &captures) const = 0;

  void addChild(Child const &child)
  {
    children_.push_back(child);
  }

  void enableCapture(std::string capture_name)
  {
    capture_name_ = capture_name;
  }

protected:
  bool fail(Value *value, Captures &captures) const;
  bool success(Value *value, Captures &captures) const;

private:
  bool matchChildren(Value *value, Captures &captures) const;
  void capture(Value *value, Captures &captures) const;
  void uncapture(Value *value, Captures &captures) const;

  std::string capture_name_{""};
  Children    children_{};
};

class AnyPattern : public OperandPrototype
{
public:
  AnyPattern();
  ~AnyPattern() override;
  bool match(Value *instr, Captures &captures) const override;
};

class CallPattern : public OperandPrototype
{
public:
  using String = std::string;
  CallPattern(String const &name);

  ~CallPattern() override;

  bool match(Value *instr, Captures &captures) const override;

private:
  String name_{};
};

template <typename T>
class InstructionPattern : public OperandPrototype
{
public:
  using OperandPrototype::OperandPrototype;
  ~InstructionPattern() override;
  bool match(Value *instr, Captures &captures) const override;
};

using LoadPattern    = InstructionPattern<llvm::LoadInst>;
using BitCastPattern = InstructionPattern<llvm::BitCastInst>;

}  // namespace quantum
}  // namespace microsoft
