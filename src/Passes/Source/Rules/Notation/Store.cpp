// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Notation/Notation.hpp"
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

        OperandPrototypePtr Store(OperandPrototypePtr target, OperandPrototypePtr value)
        {
            auto ret = std::make_shared<StorePattern>();

            ret->addChild(target);
            ret->addChild(value);
            return static_cast<OperandPrototypePtr>(ret);
        }

    } // namespace notation
} // namespace quantum
} // namespace microsoft
