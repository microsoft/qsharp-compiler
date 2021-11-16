// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Notation/Notation.hpp"
#include "Rules/Patterns/UnnamedInvokePattern.hpp"

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

        IOperandPrototypePtr unnamedInvoke()
        {
            auto ret = std::make_shared<UnnamedInvokePattern>();

            return static_cast<IOperandPrototypePtr>(ret);
        }

    } // namespace notation
} // namespace quantum
} // namespace microsoft
