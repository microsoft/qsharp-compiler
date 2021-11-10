// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/IOperandPrototype.hpp"
#include "Rules/Operands/Phi.hpp"

namespace microsoft
{
namespace quantum
{

    PhiPattern::~PhiPattern() = default;

    bool PhiPattern::match(Value* instr, Captures& captures) const
    {
        auto* phi_node = llvm::dyn_cast<llvm::PHINode>(instr);
        if (phi_node == nullptr)
        {
            return fail(instr, captures);
        }

        return success(instr, captures);
    }

    PhiPattern::Child PhiPattern::copy() const
    {
        auto ret = std::make_shared<PhiPattern>();
        ret->copyPropertiesFrom(*this);
        return std::move(ret);
    }

} // namespace quantum
} // namespace microsoft
