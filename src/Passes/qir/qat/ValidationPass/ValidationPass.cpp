// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "ValidationPass/ValidationPass.hpp"

#include "Llvm/Llvm.hpp"

#include <fstream>
#include <iostream>

namespace microsoft
{
namespace quantum
{
    llvm::PreservedAnalyses ValidationPass::run(llvm::Module& module, llvm::ModuleAnalysisManager& /*mam*/)
    {

        for (auto& function : module)
        {
            for (auto& block : function)
            {
                for (auto& instr : block)
                {
                    auto opname = instr.getOpcodeName();
                    if (opcodes_.find(opname) != opcodes_.end())
                    {
                        ++opcodes_[opname];
                    }
                    else
                    {
                        opcodes_[opname] = 1;
                    }

                    auto call_instr = llvm::dyn_cast<llvm::CallBase>(&instr);
                    if (call_instr != nullptr)
                    {
                        auto f = call_instr->getCalledFunction();
                        if (f == nullptr)
                        {
                            continue;
                        }

                        auto name = static_cast<std::string>(f->getName());
                        if (f->isDeclaration())
                        {
                            if (external_calls_.find(name) != external_calls_.end())
                            {
                                ++external_calls_[name];
                            }
                            else
                            {
                                external_calls_[name] = 1;
                            }
                        }
                        else
                        {
                            if (internal_calls_.find(name) != internal_calls_.end())
                            {
                                ++internal_calls_[name];
                            }
                            else
                            {
                                internal_calls_[name] = 1;
                            }
                        }
                    }
                }
            }
        }

        bool raise_exception = false;
        if (config_.allowlistOpcodes())
        {
            auto const& allowed_ops = config_.allowedOpcodes();
            for (auto const& k : opcodes_)
            {
                if (allowed_ops.find(k.first) == allowed_ops.end())
                {
                    logger_->error("'" + k.first + "' is not allowed for this profile.");
                }
            }
        }

        if (config_.allowlistOpcodes())
        {
            auto const& allowed_functions = config_.allowedExternalCallNames();
            for (auto const& k : external_calls_)
            {
                if (allowed_functions.find(k.first) == allowed_functions.end())
                {
                    logger_->error("'" + k.first + "' is not allowed for this profile.");
                }
            }
        }

        if (!config_.allowInternalCalls() && !internal_calls_.empty())
        {
            logger_->error("Calls to custom defined functions not allowed.");
            raise_exception = true;
        }

        if (raise_exception)
        {
            throw std::runtime_error("QIR is not valid within the defined profile");
        }

        return llvm::PreservedAnalyses::all();
    }

    bool ValidationPass::isRequired()
    {
        return true;
    }

} // namespace quantum
} // namespace microsoft
