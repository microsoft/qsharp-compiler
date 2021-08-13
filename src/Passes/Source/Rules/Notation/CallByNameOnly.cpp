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

        OperandPrototypePtr CallByNameOnly(std::string const& name)
        {
            OperandPrototypePtr ret = std::make_shared<CallPattern>(name);
            return ret;
        }

    } // namespace notation
} // namespace quantum
} // namespace microsoft
