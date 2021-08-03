#pragma once
#include "Llvm.hpp"

#include <vector>
namespace microsoft {
namespace quantum {

class InstructionPattern
{
public:
  using Instruction = llvm::Instruction;
  using String      = std::string;
  class OperandPrototype
  {
  public:
    enum MatchType
    {
      Any = 0,
      ConstantInt,
      ConstantFloat,
      ConstantBool,

      Register,
      NamedRegister,
      AnonymousRegister,

    };

    String name() const
    {
      return name_;
    }

  private:
    String name_;
    // void * value_{nullptr};
  };
  using Operands = std::vector<OperandPrototype>;

  /// @{
  InstructionPattern()                                = default;
  InstructionPattern(InstructionPattern const &other) = default;
  InstructionPattern(InstructionPattern &&other)      = default;
  InstructionPattern &operator=(InstructionPattern const &other) = default;
  InstructionPattern &operator=(InstructionPattern &&other) = default;
  /// @}

  virtual ~InstructionPattern();
  /// @{
  virtual bool match(Instruction *instr) = 0;
  /// @}

  Operands const &operands()
  {
    return operands_;
  }

private:
  Operands operands_;
};

class CallPattern : public InstructionPattern
{
public:
  using String = std::string;
  CallPattern(String const &name);

  ~CallPattern() override;

  bool match(Instruction *instr) override;

private:
  String name_{};
};

class Pattern
{
public:
  using Instruction      = llvm::Instruction;
  using MatchList        = std::vector<std::unique_ptr<InstructionPattern>>;
  using InstructionStack = std::vector<Instruction *>;

  void addPattern(std::unique_ptr<InstructionPattern> &&pattern)
  {
    patterns_.emplace_back(std::move(pattern));
  }

  bool match(InstructionStack const &stack) const
  {
    auto a = stack.size();
    auto b = patterns_.size();

    while (a != 0 && b != 0)
    {
      --a;
      --b;
      auto const &s = stack[a];
      auto const &p = patterns_[b];
      if (!p->match(s))
      {
        return false;
      }
    }

    llvm::errs() << "POSSIBLE MATCH\n";
    return true;
  }

private:
  MatchList patterns_;
};

// Propposed syntax for establishing rules
// "name"_rule  = ("add"_op(0_o, "value"_any ),
//                 "sub"_op(2_i32, "name"_reg )) => "noop"_op("value"_any, "name"_reg);
}  // namespace quantum
}  // namespace microsoft
