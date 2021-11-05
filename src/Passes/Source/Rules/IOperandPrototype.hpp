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
    /// Capturing is enabled using `captureAs(name)` which sets the name the
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

        // Constructors and destructors
        //

        IOperandPrototype() = default;
        virtual ~IOperandPrototype();

        // Interface functions
        //

        /// Interface function which determines if a given Value matches the
        /// implemented pattern. It is expected that any implementation of `match` will return a call to
        /// either `success()` or `fail()`. These functions will, in turn, ensure that the node is
        /// captured in the capture table (and erased upon backtracking) as well as matching children.
        virtual bool match(Value* value, Captures& captures) const = 0;

        /// Interface function which defines a copy operation of the underlying implementation. Note that
        /// unlike normal copy operators this operation returns a shared pointer to the new copy.
        virtual Child copy() const = 0;

        // Shared functionality
        //

        /// Adds a child to be matched against the matches children. Children
        /// are matched in order and by size.
        void addChild(Child const& child);

        /// Flags that this operand should be captured. This function ensures
        /// that the captured operand is given a name. The subsequent logic
        /// in this class is responsible for capturing (upon match) and
        /// uncapturing (upon backtrack) with specified name
        void captureAs(std::string capture_name);

      protected:
        // Function to indicate match success or failure. Either of these
        // must be called prior to return from an implementation of
        // IOperandPrototype::match.
        //

        /// Function which should be called whenever a match fails.
        bool fail(Value* value, Captures& captures) const;

        /// Function which should be called whenever a match is successful.
        bool success(Value* value, Captures& captures) const;

        // Helper functions for the capture logic.
        //

        /// Subroutine to match all children.
        bool matchChildren(Value* value, Captures& captures) const;

        // Helper functions for operation
        //

        /// Shallow copy of the operand to allow name change
        /// of the capture
        void copyPropertiesFrom(IOperandPrototype const& other)
        {
            capture_name_ = other.capture_name_;
            children_     = other.children_;
        }

      private:
        /// Captures the value into the captures table if needed.
        void capture(Value* value, Captures& captures) const;

        /// Removes any captures from the captures table upon backtracking
        void uncapture(Value* value, Captures& captures) const;

        // Data variables for common matching functionality
        //

        std::string capture_name_{""}; ///< Name to captured value. Empty means no capture.
        Children    children_{};       ///< Children to match against the values children.
    };

} // namespace quantum
} // namespace microsoft
