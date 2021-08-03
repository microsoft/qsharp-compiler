#pragma once
#include "Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

class OperandPrototype
{
public:
  using Instruction  = llvm::Instruction;
  using String       = std::string;
  using Value        = llvm::Value;
  using Child        = std::shared_ptr<OperandPrototype>;
  using Children     = std::vector<Child>;
  using Captures     = std::unordered_map<std::string, Value *>;
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

class ReplacementRule
{
public:
  using Captures            = OperandPrototype::Captures;
  using Instruction         = llvm::Instruction;
  using Value               = llvm::Value;
  using OperandPrototypePtr = std::shared_ptr<OperandPrototype>;
  using ReplaceFunction     = std::function<bool(Value *, Captures &)>;

  void setPattern(OperandPrototypePtr &&pattern)
  {
    pattern_ = std::move(pattern);
  }

  void setReplacer(ReplaceFunction const &replacer)
  {
    replacer_ = replacer;
  }

  bool match(Value *value, Captures &captures) const
  {
    if (pattern_ == nullptr)
    {
      return false;
    }
    return pattern_->match(value, captures);
  }

  bool replace(Value *value, Captures &captures) const
  {
    if (replacer_)
    {
      return replacer_(value, captures);
    }

    return false;
  }

private:
  OperandPrototypePtr pattern_{nullptr};
  ReplaceFunction     replacer_{nullptr};
};

// Propposed syntax for establishing rules
// "name"_rule  = ("add"_op(0_o, "value"_any ),
//                 "sub"_op(2_i32, "name"_reg )) => "noop"_op("value"_any, "name"_reg);
}  // namespace quantum
}  // namespace microsoft
