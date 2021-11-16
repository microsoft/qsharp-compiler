// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/IOperandPrototype.hpp"

namespace microsoft
{
namespace quantum
{

    IOperandPrototype::~IOperandPrototype() = default;
    bool IOperandPrototype::matchChildren(Value* value, Captures& captures) const
    {
        if (!children_.empty())
        {
            auto user = llvm::dyn_cast<llvm::User>(value);

            if (user == nullptr)
            {
                return false;
            }

            if (user->getNumOperands() != children_.size())
            {
                return false;
            }

            uint64_t i = 0;
            while (i < children_.size())
            {
                auto v = user->getOperand(static_cast<uint32_t>(i));
                if (!children_[i]->match(v, captures))
                {
                    return false;
                }
                ++i;
            }

            return true;
        }

        // TODO(QAT-private-issue-33): value may be other type than llvm::User. Check other relevant types
        // and deal with it.

        return true;
    }

    void IOperandPrototype::addChild(Child const& child)
    {
        children_.push_back(child);
    }

    void IOperandPrototype::captureAs(std::string capture_name)
    {
        capture_name_ = std::move(capture_name);
    }

    bool IOperandPrototype::fail(Value* /*value*/, Captures& /*captures*/) const
    {
        return false;
    }

    bool IOperandPrototype::success(Value* value, Captures& captures) const
    {
        capture(value, captures);

        auto ret = matchChildren(value, captures);
        if (!ret)
        {
            uncapture(value, captures);
        }
        return ret;
    }

    void IOperandPrototype::capture(Value* value, Captures& captures) const
    {
        if (!capture_name_.empty())
        {
            captures[capture_name_] = value;
        }
    }

    void IOperandPrototype::uncapture(Value* /*value*/, Captures& captures) const
    {
        if (!capture_name_.empty())
        {
            auto it = captures.find(capture_name_);
            if (it == captures.end())
            {
                throw std::runtime_error("Previously captured name " + capture_name_ + " not found in capture list.");
            }

            captures.erase(it);
        }
    }

} // namespace quantum
} // namespace microsoft
