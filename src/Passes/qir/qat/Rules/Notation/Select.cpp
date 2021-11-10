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

        using IOperandPrototypePtr = std::shared_ptr<IOperandPrototype>;

        IOperandPrototypePtr select(
            IOperandPrototypePtr const& cond,
            IOperandPrototypePtr const& arg1,
            IOperandPrototypePtr const& arg2)
        {
            auto select_pattern = std::make_shared<SelectPattern>();

            select_pattern->addChild(cond);
            select_pattern->addChild(arg1);
            select_pattern->addChild(arg2);

            return static_cast<IOperandPrototypePtr>(select_pattern);
        }

    } // namespace notation
} // namespace quantum
} // namespace microsoft
