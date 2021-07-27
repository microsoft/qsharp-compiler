// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

#include "QubitAllocationAnalysis/QubitAllocationAnalysis.hpp"

#include <fstream>
#include <iostream>
#include <unordered_set>

namespace microsoft
{
namespace quantum
{

    bool QubitAllocationAnalysisAnalytics::operandsConstant(Instruction const& instruction) const
    {
        // Default is true (i.e. the case of no operands)
        bool ret = true;

        // Checking that all oprands are constant
        for (auto& op : instruction.operands())
        {

            // An operand is constant if its value was previously generated from
            // a const expression ...
            auto const_arg = constantness_dependencies_.find(op) != constantness_dependencies_.end();

            // ... or if it is just a compile time constant. Note that we
            // delibrately only consider integers. We may expand this
            // to other constants once we have function support.
            auto cst         = llvm::dyn_cast<llvm::ConstantInt>(op);
            auto is_constant = (cst != nullptr);

            ret = ret && (const_arg || is_constant);
        }

        return ret;
    }

    void QubitAllocationAnalysisAnalytics::markPossibleConstant(Instruction& instruction)
    {
        // Creating arg dependencies
        ArgList all_dependencies{};
        for (auto& op : instruction.operands())
        {
            // If the operand has dependecies ...
            auto it = constantness_dependencies_.find(op);
            if (it != constantness_dependencies_.end())
            {
                // ...  we add these as a dependency for the
                // resulting instructions value
                for (auto& arg : it->second)
                {
                    all_dependencies.insert(arg);
                }
            }
        }

        // Adding full list of dependices to the dependency graph
        constantness_dependencies_.insert({&instruction, all_dependencies});
    }

    void QubitAllocationAnalysisAnalytics::analyseCall(Instruction& instruction)
    {
        // Skipping debug code
        if (instruction.isDebugOrPseudoInst())
        {
            return;
        }

        // Recovering the call information
        auto* call_instr = llvm::dyn_cast<llvm::CallBase>(&instruction);
        if (call_instr == nullptr)
        {
            return;
        }

        // Getting the name of the function being called
        auto target_function = call_instr->getCalledFunction();
        auto name            = target_function->getName();

        // TODO(tfr): Make use of TargetLibraryInfo
        if (name != "__quantum__rt__qubit_allocate_array")
        {
            return;
        }

        // We expect only a single argument with the number
        // of qubits allocated
        if (call_instr->arg_size() != 1)
        {
            llvm::errs() << "Expected exactly one argument\n";
            return;
        }

        // Next we extract the argument ...
        auto argument = call_instr->getArgOperand(0);
        if (argument == nullptr)
        {
            llvm::errs() << "Failed getting the size argument\n";
            return;
        }

        // ... and checks whether it is a result of a dependant
        // const expression
        auto it = constantness_dependencies_.find(argument);
        if (it != constantness_dependencies_.end())
        {
            // If it is, we add the details to the result list
            QubitArray qubit_array;
            qubit_array.is_possibly_static = true;
            qubit_array.variable_name      = instruction.getName().str();
            qubit_array.depends_on         = it->second;

            // Pushing to the result
            results_.push_back(std::move(qubit_array));
            return;
        }

        // Otherwise, it may be a static allocation based on a constant (or
        // folded constant)
        auto cst = llvm::dyn_cast<llvm::ConstantInt>(argument);
        if (cst != nullptr)
        {
            QubitArray qubit_array;
            qubit_array.is_possibly_static = true;
            qubit_array.variable_name      = instruction.getName().str();
            qubit_array.size               = cst->getZExtValue();

            // Pushing to the result
            results_.push_back(std::move(qubit_array));

            return;
        }

        // If neither of the previous is the case, we are dealing with a non-static array
        QubitArray qubit_array;
        qubit_array.is_possibly_static = false;
        qubit_array.variable_name      = instruction.getName().str();

        // Storing the result
        results_.push_back(std::move(qubit_array));
    }

    void QubitAllocationAnalysisAnalytics::analyseFunction(llvm::Function& function)
    {
        // Clearing results generated in a previous run
        results_.clear();
        constantness_dependencies_.clear();

        // Creating a list with function arguments
        for (auto& arg : function.args())
        {
            auto s = arg.getName().str();
            constantness_dependencies_.insert({&arg, {s}});
        }

        // Evaluating all expressions
        for (auto& basic_block : function)
        {
            for (auto& instruction : basic_block)
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

    QubitAllocationAnalysisAnalytics::Result QubitAllocationAnalysisAnalytics::run(
        llvm::Function& function,
        llvm::FunctionAnalysisManager& /*unused*/)
    {
        // Running functin analysis
        analyseFunction(function);

        // ... and return the result.
        return results_;
    }

    QubitAllocationAnalysisPrinter::QubitAllocationAnalysisPrinter(llvm::raw_ostream& out_stream)
      : out_stream_(out_stream)
    {
    }

    llvm::PreservedAnalyses QubitAllocationAnalysisPrinter::run(
        llvm::Function&                function,
        llvm::FunctionAnalysisManager& fam)
    {
        auto& results = fam.getResult<QubitAllocationAnalysisAnalytics>(function);

        if (!results.empty())
        {
            out_stream_ << function.getName() << "\n";
            out_stream_ << "===================="
                        << "\n\n";
            for (auto const& ret : results)
            {
                if (!ret.is_possibly_static)
                {
                    out_stream_ << ret.variable_name << " is dynamic.\n";
                }
                else
                {
                    if (ret.depends_on.empty())
                    {
                        out_stream_ << ret.variable_name << " is trivially static with " << ret.size << " qubits.";
                    }
                    else
                    {
                        out_stream_ << ret.variable_name << " depends on ";
                        bool first = true;
                        for (auto& x : ret.depends_on)
                        {
                            if (!first)
                            {
                                out_stream_ << ", ";
                            }
                            out_stream_ << x;
                            first = false;
                        }
                        out_stream_ << " being constant to be static.";
                    }
                }

                out_stream_ << "\n";
            }
        }

        return llvm::PreservedAnalyses::all();
    }

    bool QubitAllocationAnalysisPrinter::isRequired()
    {
        return true;
    }

    llvm::AnalysisKey QubitAllocationAnalysisAnalytics::Key;

} // namespace quantum
} // namespace microsoft
