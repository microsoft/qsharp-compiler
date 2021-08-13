// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Notation/Notation.hpp"
#include "Rules/Operands/Any.hpp"
#include "Rules/Operands/Call.hpp"
#include "Rules/Operands/Instruction.hpp"

#include "Llvm/Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{
    namespace notation
    {

        using OperandPrototypePtr = std::shared_ptr<OperandPrototype>;

        OperandPrototypePtr BitCast(OperandPrototypePtr arg)
        {
            auto cast_pattern = std::make_shared<BitCastPattern>();

            cast_pattern->addChild(arg);
            return static_cast<OperandPrototypePtr>(cast_pattern);
        }

    } // namespace notation
} // namespace quantum
} // namespace microsoft
