// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Notation/Notation.hpp"
#include "Rules/Patterns/AnyPattern.hpp"
#include "Rules/Patterns/CallPattern.hpp"
#include "Rules/Patterns/Instruction.hpp"

#include "Llvm/Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{
    namespace notation
    {

        using IOperandPrototypePtr = std::shared_ptr<IOperandPrototype>;

        IOperandPrototypePtr constInt()
        {
            auto cast_pattern = std::make_shared<ConstIntPattern>();

            return static_cast<IOperandPrototypePtr>(cast_pattern);
        }

    } // namespace notation
} // namespace quantum
} // namespace microsoft
