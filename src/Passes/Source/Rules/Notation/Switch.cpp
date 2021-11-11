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

        IOperandPrototypePtr switchOp(
            IOperandPrototypePtr const& /*cond*/,
            IOperandPrototypePtr const& /*arg1*/,
            IOperandPrototypePtr const& /*arg2*/)
        {
            auto switch_pattern = std::make_shared<SwitchPattern>();

            // TODO(tfr): finish switch pattern
            //  switch_pattern->addChild(cond);
            // switch_pattern->addChild(arg1);
            //  switch_pattern->addChild(arg2);

            return static_cast<IOperandPrototypePtr>(switch_pattern);
        }

    } // namespace notation
} // namespace quantum
} // namespace microsoft
