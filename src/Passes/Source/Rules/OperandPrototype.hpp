#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    /// IOperandPrototype describes an IR pattern and allows matching against
    /// LLVMs llvm::Value type. Each value may or may not be captured during the
    /// matching process which means that they are stored in a map under a given name.
    /// Capturing is enabled using `enableCapture(name)` which sets the name the
    /// value should be stored under.
    class IOperandPrototype
    {
      public:
        using Instruction = llvm::Instruction;
        using String      = std::string;
        using Value       = llvm::Value;
        using Child       = std::shared_ptr<IOperandPrototype>;
        using Children    = std::vector<Child>;
        using Captures    = std::unordered_map<std::string, Value*>;

        /// Constructors and desctructors
        /// @{
        IOperandPrototype() = default;
        virtual ~IOperandPrototype();
        /// @}

        /// Interface functions
        /// @{
        virtual bool  match(Value* value, Captures& captures) const = 0;
        virtual Child copy() const                                  = 0;
        /// @}

        /// Shared functionality
        /// @{

        /// Adds a child to be matched against the matchees children. Children
        /// are matched in order and by size.
        void addChild(Child const& child);

        /// Flags that this operand should be captured. This function ensures
        /// that the captured operand is given a name. The subsequent logic
        /// in this class is responsible for capturing (upon match) and
        /// uncapturing (upon backtrack) with specified name
        void enableCapture(std::string capture_name);
        /// @}
      protected:
        /// Function to indicate match success or failure. Either of these
        /// must be called prior to return from an implementation of
        /// IOperandPrototype::match.
        /// @{
        bool fail(Value* value, Captures& captures) const;
        bool success(Value* value, Captures& captures) const;
        /// @}

        /// Helper functions for the capture logic.
        /// @{
        bool matchChildren(Value* value, Captures& captures) const;
        void capture(Value* value, Captures& captures) const;
        void uncapture(Value* value, Captures& captures) const;
        /// @}

        /// Helper functions for operation
        /// @{
        /// Shallow copy of the operand to allow name change
        /// of the capture
        void copyPropertiesFrom(IOperandPrototype const& other)
        {
            capture_name_ = other.capture_name_;
            children_     = other.children_;
        }
        /// @}

      private:
        /// Data variables for common matching functionality
        /// @{
        std::string capture_name_{""}; ///< Name to captured value. Empty means no capture.
        Children    children_{};       ///< Children to match aginst the values children.
                                       /// @}
    };

} // namespace quantum
} // namespace microsoft
