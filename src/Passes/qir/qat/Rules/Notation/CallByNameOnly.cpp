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

        IOperandPrototypePtr callByNameOnly(std::string const& name)
        {
            IOperandPrototypePtr ret = std::make_shared<CallPattern>(name);
            return ret;
        }

    } // namespace notation
} // namespace quantum
} // namespace microsoft
