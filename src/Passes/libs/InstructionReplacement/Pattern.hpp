#pragma once
#include "Llvm.hpp"

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
  OperandPrototype(bool capture = false, std::string const &capture_name = "")
    : capture_{capture}
    , capture_name_{capture_name}
  {}
  virtual ~OperandPrototype();
  virtual bool match(Value *value) const = 0;

  bool capture() const
  {
    return capture_;
  }

  void addChild(Child const &child)
  {
    children_.push_back(child);
  }

protected:
  bool matchChildren(Value *value) const;

private:
  bool        capture_{false};
  std::string capture_name_{""};
  Children    children_{};
};

class CallPattern : public OperandPrototype
{
public:
  using String = std::string;
  CallPattern(String const &name);

  ~CallPattern() override;

  bool match(Value *instr) const override;

private:
  String name_{};
};

class Pattern
{
public:
  using Instruction         = llvm::Instruction;
  using Value               = llvm::Value;
  using InstructionStack    = std::vector<Instruction *>;
  using OperandPrototypePtr = std::shared_ptr<OperandPrototype>;
  void addPattern(OperandPrototypePtr &&pattern)
  {
    pattern_ = std::move(pattern);
  }

  bool match(Value *value) const
  {
    if (pattern_ == nullptr)
    {
      return false;
    }
    return pattern_->match(value);
  }

private:
  OperandPrototypePtr pattern_{nullptr};
};

// Propposed syntax for establishing rules
// "name"_rule  = ("add"_op(0_o, "value"_any ),
//                 "sub"_op(2_i32, "name"_reg )) => "noop"_op("value"_any, "name"_reg);
}  // namespace quantum
}  // namespace microsoft
