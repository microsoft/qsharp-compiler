#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Notation/Call.ipp"
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

        class Capture
        {
          public:
            explicit Capture(std::string const& name);
            // Note that this operator is delibrately unconventional
            IOperandPrototypePtr operator=(IOperandPrototypePtr const& other); // NOLINT

          private:
            std::string name_{};
        };

        /// @{
        template <typename... Args> IOperandPrototypePtr call(std::string const& name, Args... args);
        IOperandPrototypePtr                             callByNameOnly(std::string const& name);
        IOperandPrototypePtr                             bitCast(IOperandPrototypePtr const& arg);
        IOperandPrototypePtr                             branch(
                                        IOperandPrototypePtr const& cond,
                                        IOperandPrototypePtr const& arg1,
                                        IOperandPrototypePtr const& arg2);
        IOperandPrototypePtr load(IOperandPrototypePtr const& arg);
        IOperandPrototypePtr store(IOperandPrototypePtr const& target, IOperandPrototypePtr const& value);
        /// @}

        /// @{
        static std::shared_ptr<AnyPattern> _ = std::make_shared<AnyPattern>();
        /// @}

        /// @{
        std::function<bool(
            ReplacementRule::Builder&,
            ReplacementRule::Value*,
            ReplacementRule::Captures&,
            ReplacementRule::Replacements&)>
        deleteInstruction();

        /// @}

        Capture operator""_cap(char const* name, std::size_t);

    } // namespace notation
} // namespace quantum
} // namespace microsoft
