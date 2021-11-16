#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/IOperandPrototype.hpp"

#include "Llvm/Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    class StorePattern : public IOperandPrototype
    {
      public:
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class LoadPattern : public IOperandPrototype
    {
      public:
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class BitCastPattern : public IOperandPrototype
    {
      public:
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class IntToPtrPattern : public IOperandPrototype
    {
      public:
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class ConstIntPattern : public IOperandPrototype
    {
      public:
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class BranchPattern : public IOperandPrototype
    {
      public:
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class SelectPattern : public IOperandPrototype
    {
      public:
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class BasicBlockPattern : public IOperandPrototype
    {
      public:
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class SwitchPattern : public IOperandPrototype
    {
      public:
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

} // namespace quantum
} // namespace microsoft
