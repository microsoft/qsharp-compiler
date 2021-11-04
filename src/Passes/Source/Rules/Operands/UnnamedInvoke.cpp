// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/IOperandPrototype.hpp"
#include "Rules/Operands/UnnamedInvoke.hpp"

namespace microsoft
{
namespace quantum
{

    UnnamedInvokePattern::~UnnamedInvokePattern() = default;

    bool UnnamedInvokePattern::match(Value* instr, Captures& captures) const
    {
        auto* call_instr = llvm::dyn_cast<llvm::InvokeInst>(instr);
        if (call_instr == nullptr)
        {
            return fail(instr, captures);
        }

        return success(instr, captures);
    }

    UnnamedInvokePattern::Child UnnamedInvokePattern::copy() const
    {
        auto ret = std::make_shared<UnnamedInvokePattern>();
        ret->copyPropertiesFrom(*this);
        return std::move(ret);
    }

} // namespace quantum
} // namespace microsoft
