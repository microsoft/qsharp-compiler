#include "InstructionReplacement/Pattern.hpp"

namespace microsoft {
namespace quantum {

InstructionPattern::~InstructionPattern() = default;

CallPattern::CallPattern(String const &name)
  : name_{name}
{}
CallPattern::~CallPattern() = default;

bool CallPattern::match(Instruction *instr)
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

  // TODO: Check operands
  llvm::errs() << "Found call to " << name << "\n";

  return true;
}

}  // namespace quantum
}  // namespace microsoft
