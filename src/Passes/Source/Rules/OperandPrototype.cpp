// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/OperandPrototype.hpp"

namespace microsoft
{
namespace quantum
{

    IOperandPrototype::~IOperandPrototype() = default;
    bool IOperandPrototype::matchChildren(Value* value, Captures& captures) const
    {
        auto user = llvm::dyn_cast<llvm::User>(value);
        if (!children_.empty())
        {
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

        //  llvm::errs() << "SUCCESS MATCH: " << *value << " " << user->getNumOperands() << "\n";
        // TODO(tfr): Check other possibilities for value

        return true;
    }

    void IOperandPrototype::addChild(Child const& child)
    {
        children_.push_back(child);
    }

    void IOperandPrototype::enableCapture(std::string capture_name)
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
            captures.erase(captures.find(capture_name_));
        }
    }

} // namespace quantum
} // namespace microsoft
