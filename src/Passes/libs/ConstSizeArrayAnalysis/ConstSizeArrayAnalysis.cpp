// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "ConstSizeArrayAnalysis/ConstSizeArrayAnalysis.hpp"

#include "Llvm.hpp"

#include <fstream>
#include <iostream>
#include <unordered_set>

namespace microsoft {
namespace quantum {

bool ConstSizeArrayAnalysisAnalytics::operandsConstant(Instruction const &instruction) const
{
  bool ret = true;

  // Checking that all oprands are constant
  for (auto &op : instruction.operands())
  {

    auto const_arg   = value_depending_on_args_.find(op) != value_depending_on_args_.end();
    auto cst         = llvm::dyn_cast<llvm::ConstantInt>(op);
    auto is_constant = (cst != nullptr);

    ret = ret && (const_arg || is_constant);
  }

  return ret;
}

void ConstSizeArrayAnalysisAnalytics::markPossibleConstant(Instruction &instruction)
{
  /*
  // Rename constant variables
  if (!instruction.hasName())
  {
    // Naming result
    char new_name[64] = {0};
    auto fmt          = llvm::format("microsoft_reserved_possible_const_ret%u", tmp_counter_);
    fmt.print(new_name, 64);
    instruction.setName(new_name);
  }
  */

  // Creating arg dependencies
  ArgList all_dependencies{};
  for (auto &op : instruction.operands())
  {
    auto it = value_depending_on_args_.find(op);
    if (it != value_depending_on_args_.end())
    {
      for (auto &arg : it->second)
      {
        all_dependencies.insert(arg);
      }
    }
  }

  // Adding the new name to the list
  value_depending_on_args_.insert({&instruction, all_dependencies});
}

void ConstSizeArrayAnalysisAnalytics::analyseCall(Instruction &instruction)
{
  // Skipping debug code
  if (instruction.isDebugOrPseudoInst())
  {
    return;
  }

  auto *call_instr = llvm::dyn_cast<llvm::CallBase>(&instruction);
  if (call_instr == nullptr)
  {
    return;
  }

  auto target_function = call_instr->getCalledFunction();
  auto name            = target_function->getName();

  // TODO(tfr): Make use of TargetLibrary
  if (name != "__quantum__rt__qubit_allocate_array")
  {
    return;
  }

  if (call_instr->arg_size() != 1)
  {
    llvm::errs() << "Expected exactly one argument\n";
    return;
  }

  auto argument = call_instr->getArgOperand(0);
  if (argument == nullptr)
  {
    llvm::errs() << "Failed getting the size argument\n";
    return;
  }

  // Checking named values
  auto it = value_depending_on_args_.find(argument);
  if (it != value_depending_on_args_.end())
  {
    QubitArray qubit_array;
    qubit_array.is_possibly_static = true;
    qubit_array.variable_name      = instruction.getName().str();
    qubit_array.depends_on         = it->second;

    // Pushing to the result
    results_.push_back(std::move(qubit_array));
    return;
  }

  // Checking if it is a constant value
  auto cst = llvm::dyn_cast<llvm::ConstantInt>(argument);
  if (cst != nullptr)
  {
    QubitArray qubit_array;
    qubit_array.is_possibly_static = true;
    qubit_array.variable_name      = instruction.getName().str();

    // Pushing to the result
    results_.push_back(std::move(qubit_array));

    return;
  }

  // Non-static array
  QubitArray qubit_array;
  qubit_array.is_possibly_static = false;
  qubit_array.variable_name      = instruction.getName().str();
  results_.push_back(std::move(qubit_array));
}

void ConstSizeArrayAnalysisAnalytics::analyseFunction(llvm::Function &function)
{
  results_.clear();

  // Creating a list with function arguments
  for (auto &arg : function.args())
  {
    auto s = arg.getName().str();
    value_depending_on_args_.insert({&arg, {s}});
  }

  // Evaluating all expressions
  for (auto &basic_block : function)
  {
    for (auto &instruction : basic_block)
    {

      auto opcode = instruction.getOpcode();
      switch (opcode)
      {
      case llvm::Instruction::Sub:
      case llvm::Instruction::Add:
      case llvm::Instruction::Mul:
      case llvm::Instruction::Shl:
      case llvm::Instruction::LShr:
      case llvm::Instruction::AShr:
      case llvm::Instruction::And:
      case llvm::Instruction::Or:
      case llvm::Instruction::Xor:
        if (operandsConstant(instruction))
        {
          markPossibleConstant(instruction);
        }
        break;
      case llvm::Instruction::Call:
        analyseCall(instruction);
        break;
        // Unanalysed statements
      case llvm::Instruction::Ret:
      case llvm::Instruction::Br:
      case llvm::Instruction::Switch:
      case llvm::Instruction::IndirectBr:
      case llvm::Instruction::Invoke:
      case llvm::Instruction::Resume:
      case llvm::Instruction::Unreachable:
      case llvm::Instruction::CleanupRet:
      case llvm::Instruction::CatchRet:
      case llvm::Instruction::CatchSwitch:
      case llvm::Instruction::CallBr:
      case llvm::Instruction::FNeg:
      case llvm::Instruction::FAdd:
      case llvm::Instruction::FSub:
      case llvm::Instruction::FMul:
      case llvm::Instruction::UDiv:
      case llvm::Instruction::SDiv:
      case llvm::Instruction::FDiv:
      case llvm::Instruction::URem:
      case llvm::Instruction::SRem:
      case llvm::Instruction::FRem:
      case llvm::Instruction::Alloca:
      case llvm::Instruction::Load:
      case llvm::Instruction::Store:
      case llvm::Instruction::GetElementPtr:
      case llvm::Instruction::Fence:
      case llvm::Instruction::AtomicCmpXchg:
      case llvm::Instruction::AtomicRMW:
      case llvm::Instruction::Trunc:
      case llvm::Instruction::ZExt:
      case llvm::Instruction::SExt:
      case llvm::Instruction::FPToUI:
      case llvm::Instruction::FPToSI:
      case llvm::Instruction::UIToFP:
      case llvm::Instruction::SIToFP:
      case llvm::Instruction::FPTrunc:
      case llvm::Instruction::FPExt:
      case llvm::Instruction::PtrToInt:
      case llvm::Instruction::IntToPtr:
      case llvm::Instruction::BitCast:
      case llvm::Instruction::AddrSpaceCast:
      case llvm::Instruction::CleanupPad:
      case llvm::Instruction::CatchPad:
      case llvm::Instruction::ICmp:
      case llvm::Instruction::FCmp:
      case llvm::Instruction::PHI:
      case llvm::Instruction::Select:
      case llvm::Instruction::UserOp1:
      case llvm::Instruction::UserOp2:
      case llvm::Instruction::VAArg:
      case llvm::Instruction::ExtractElement:
      case llvm::Instruction::InsertElement:
      case llvm::Instruction::ShuffleVector:
      case llvm::Instruction::ExtractValue:
      case llvm::Instruction::InsertValue:
      case llvm::Instruction::LandingPad:
        // End of Binary Ops
      default:
        break;
      }
    }
  }
}

ConstSizeArrayAnalysisAnalytics::Result ConstSizeArrayAnalysisAnalytics::run(
    llvm::Function &function, llvm::FunctionAnalysisManager & /*unused*/)
{
  analyseFunction(function);

  return results_;
}

ConstSizeArrayAnalysisPrinter::ConstSizeArrayAnalysisPrinter(llvm::raw_ostream &out_stream)
  : out_stream_(out_stream)
{}

llvm::PreservedAnalyses ConstSizeArrayAnalysisPrinter::run(llvm::Function &               function,
                                                           llvm::FunctionAnalysisManager &fam)
{
  auto &results = fam.getResult<ConstSizeArrayAnalysisAnalytics>(function);

  if (!results.empty())
  {
    out_stream_ << function.getName() << "\n";
    out_stream_ << "===================="
                << "\n\n";
    for (auto const &ret : results)
    {
      out_stream_ << ret.variable_name << (ret.is_possibly_static ? ": " : "!");
      for (auto &x : ret.depends_on)
      {
        out_stream_ << x << ", ";
      }
      out_stream_ << "\n";
    }
  }

  return llvm::PreservedAnalyses::all();
}

bool ConstSizeArrayAnalysisPrinter::isRequired()
{
  return true;
}

llvm::AnalysisKey ConstSizeArrayAnalysisAnalytics::Key;

}  // namespace quantum
}  // namespace microsoft
