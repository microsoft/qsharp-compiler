#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/OperandPrototype.hpp"

#include "Llvm/Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    class AnyPattern : public IOperandPrototype
    {
      public:
        AnyPattern();
        ~AnyPattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

} // namespace quantum
} // namespace microsoft
