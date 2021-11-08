// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/IOperandPrototype.hpp"
#include "Rules/Patterns/AnyPattern.hpp"

namespace microsoft
{
namespace quantum
{

    AnyPattern::AnyPattern()  = default;
    AnyPattern::~AnyPattern() = default;
    bool AnyPattern::match(Value* instr, Captures& captures) const
    {
        return success(instr, captures);
    }

    AnyPattern::Child AnyPattern::copy() const
    {
        return std::make_shared<AnyPattern>();
    }

} // namespace quantum
} // namespace microsoft
