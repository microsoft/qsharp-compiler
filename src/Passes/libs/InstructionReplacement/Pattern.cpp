#include "InstructionReplacement/Pattern.hpp"

namespace microsoft {
namespace quantum {

CallPattern::CallPattern(String const &name)
  : name_{name}
{}
CallPattern::~CallPattern() = default;

bool CallPattern::match(Value *instr) const
{
  auto *call_instr = llvm::dyn_cast<llvm::CallBase>(instr);
  if (call_instr == nullptr)
  {
    return false;
  }

  auto target_function = call_instr->getCalledFunction();
  auto name            = target_function->getName();

  if (name != name_)
  {
    return false;
  }

  return matchChildren(instr);
}

OperandPrototype::~OperandPrototype() = default;
bool OperandPrototype::matchChildren(Value *value) const
{
  if (!children_.empty())
  {
    auto user = llvm::dyn_cast<llvm::User>(value);
    if (user == nullptr)
    {
      return false;
    }

    if (user->getNumOperands() != children_.size())
    {
      return false;
    }

    uint64_t i = 0;
    while (i < children_.size())
    {
      auto v = user->getOperand(static_cast<uint32_t>(i));
      if (!children_[i]->match(v))
      {
        return false;
      }
      ++i;
    }

    return true;
  }

  // TODO: Check other possibilities for value

  return true;
}

}  // namespace quantum
}  // namespace microsoft
