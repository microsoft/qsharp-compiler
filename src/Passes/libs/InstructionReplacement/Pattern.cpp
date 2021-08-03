#include "InstructionReplacement/Pattern.hpp"

namespace microsoft {
namespace quantum {

OperandPrototype::~OperandPrototype() = default;
bool OperandPrototype::matchChildren(Value *value, Captures &captures) const
{
  auto user = llvm::dyn_cast<llvm::User>(value);
  if (!children_.empty())
  {

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
      if (!children_[i]->match(v, captures))
      {
        return false;
      }
      ++i;
    }

    //    llvm::errs() << "SUCCESS MATCH: " << *value << "\n";

    return true;
  }

  //  llvm::errs() << "SUCCESS MATCH: " << *value << " " << user->getNumOperands() << "\n";
  // TODO: Check other possibilities for value

  return true;
}

bool OperandPrototype::fail(Value * /*value*/, Captures & /*captures*/) const
{
  return false;
}

bool OperandPrototype::success(Value *value, Captures &captures) const
{
  capture(value, captures);

  auto ret = matchChildren(value, captures);
  if (!ret)
  {
    uncapture(value, captures);
  }
  return ret;
}

void OperandPrototype::capture(Value *value, Captures &captures) const
{
  if (!capture_name_.empty())
  {
    captures[capture_name_] = value;
  }
}

void OperandPrototype::uncapture(Value * /*value*/, Captures &captures) const
{
  if (!capture_name_.empty())
  {
    captures.erase(captures.find(capture_name_));
  }
}

CallPattern::CallPattern(String const &name)
  : name_{name}
{}

CallPattern::~CallPattern() = default;

bool CallPattern::match(Value *instr, Captures &captures) const
{
  auto *call_instr = llvm::dyn_cast<llvm::CallBase>(instr);
  if (call_instr == nullptr)
  {
    return fail(instr, captures);
  }

  auto target_function = call_instr->getCalledFunction();
  auto name            = target_function->getName();

  if (name != name_)
  {
    return fail(instr, captures);
  }

  return success(instr, captures);
}

AnyPattern::AnyPattern()  = default;
AnyPattern::~AnyPattern() = default;
bool AnyPattern::match(Value *instr, Captures &captures) const
{
  return success(instr, captures);
}

template <typename T>
InstructionPattern<T>::~InstructionPattern() = default;
template <typename T>
bool InstructionPattern<T>::match(Value *instr, Captures &captures) const
{
  auto *load_instr = llvm::dyn_cast<T>(instr);
  if (load_instr == nullptr)
  {
    return fail(instr, captures);
  }

  return success(instr, captures);
}

// TODO(tfr): This seems to be a bug in LLVM. Template instantiations in
// a single translation unit is not supposed to reinstantiate across other
// translation units.
//
// However, it is suspecious that htis problem has been around since Clang 8.
// so this needs more investigation. For now, this work around suffices
// See
// https://bugs.llvm.org/show_bug.cgi?id=18733
// https://stackoverflow.com/questions/56041900/why-does-explicit-template-instantiation-result-in-weak-template-vtables-warning
// for more information
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wweak-template-vtables"
template class InstructionPattern<llvm::LoadInst>;
template class InstructionPattern<llvm::BitCastInst>;
#pragma clang diagnostic pop

}  // namespace quantum
}  // namespace microsoft
