#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// @defgroup shorthandNotation Shorthand Notation

#include "Llvm/Llvm.hpp"
#include "Rules/Notation/Call.ipp"
#include "Rules/Operands/Any.hpp"
#include "Rules/Operands/Call.hpp"
#include "Rules/Operands/Instruction.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {
/// Shorthand notations to make it easy and readible to create patterns.
///
///
namespace notation {

using IOperandPrototypePtr = std::shared_ptr<IOperandPrototype>;
using ReplacerFunction =
    std::function<bool(ReplacementRule::Builder &, ReplacementRule::Value *,
                       ReplacementRule::Captures &, ReplacementRule::Replacements &)>;

/// Helper class to enable literals for IR patterns. The main purpose of this class
/// is to enable notation that allows one write `"name"_cap = operandGenerator()` where
/// the operand generator is a function which creates a IOperandPrototypePtr. This notation
/// means that whenever a operand is matched, the matched value is stored under "name".
class Capture
{
public:
  /// Explicit creation using string name constructor.
  explicit Capture(std::string const &name);

  // Note that this operator is delibrately unconventional
  IOperandPrototypePtr operator=(IOperandPrototypePtr const &other);  // NOLINT

private:
  std::string name_{};  ///< Name that is assigned to the IOperandPrototype
};

/// @addtogroup shorthandNotation
/// @{
/// Shorthand notations are made to make it possible to match patterns in the QIR. This part of the
/// library focuses on making it easy to express advance patterns in just a few lines and specify
/// what parts of the IR is of interest to the replacer function. An example is following pattern
///
/// ```
/// auto get_one = call("__quantum__rt__result_get_one");
/// addRule(
///     {branch("cond"_cap = call("__quantum__rt__result_equal", "result"_cap = _, "one"_cap =
///     get_one), _, _),
///      replace_branch_positive});
///
/// ```
///
/// which matches IRs of the form
///
/// ```
/// %1 = call %Result* @__quantum__rt__result_get_one()
/// %2 = call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
/// br i1 %2, label %then0__1, label %continue__1
/// ```
///
/// The pattern futher specifies that as a successful match is obtained, a table capturing
/// certain values must be created. In the above example, the table would contain three
/// entries: `cond`, `result` and `one` each of which would point to a a llvm::Value*
/// in the QIR. This allows the replacement function to easily manipulate the DAG in these
/// three places (four if you include the main captured value which is always passed to the
/// replacement function).

/// Shorthand notation to match an instruction for a function call.
/// The resulting IOperandPrototype matches a function call with arguments
/// as specified by the arguments given. For instance,
///
/// ```
/// addRule({call("foo", _, _), deleteInstruction()});
/// ```
///
/// matches a call to the function `foo` with exactly two arguments.
template <typename... Args>
IOperandPrototypePtr call(std::string const &name, Args... args);

/// Shorthand notation to match an instruction with a specified name.
/// Unlike call, this pattern matches by name only and ignore
/// the arguments.
///
/// ```
/// addRule({callByNameOnly("foo"), deleteInstruction()});
/// ```
///
/// matches calls to the function `foo` regardless of the number of arguments.
IOperandPrototypePtr callByNameOnly(std::string const &name);

/// Matches the llvm::BitCast instructruction.
IOperandPrototypePtr bitCast(IOperandPrototypePtr const &arg);

/// Matches the llvm::IntToPtr instructruction.
IOperandPrototypePtr intToPtr(IOperandPrototypePtr const &arg);

/// Matches the llvm::ConstantInt instructruction.
IOperandPrototypePtr constInt();

/// Matches a branch instruction given a condition and two arguments.
IOperandPrototypePtr branch(IOperandPrototypePtr const &cond, IOperandPrototypePtr const &arg1,
                            IOperandPrototypePtr const &arg2);

/// Matches a load instruction with one argument.
IOperandPrototypePtr load(IOperandPrototypePtr const &arg);

/// Matches a store instruction with a target and a value.
/// ```
/// addRule({store("target"_cap = _, "value"_cap = _), replaceConstExpr});
/// ```
/// where we want to match all store instructions and do not really care about how the target or
/// value came about. In this case, we may want to capture the values to, for instance, make
/// assignment at compile time.
IOperandPrototypePtr store(IOperandPrototypePtr const &target, IOperandPrototypePtr const &value);
/// @}

/// @addtogroup shorthandNotation
/// @{
/// The module further has shorthand notation for often encountered patterns such as any operand.

/// Shorthand notation for a wildcard which matches anything. This value
/// is useful when for instance capturing the arguments of a function call where the
/// origin of the value does not matter to the pattern.
static std::shared_ptr<AnyPattern> const _ = std::make_shared<AnyPattern>();  // NOLINT

/// @}

/// @addtogroup shorthandNotation
/// @{
/// The module also implements shorthand notation for common replacers.

/// Shorthand notation to delete an instruction. If passed as the replacement function, this
/// function generates a replacer that deletes the instruction. This is a shorthand notation for
/// deleting an instruction that can be used with a custom rule when building a ruleset. This
/// function can be used with shorthand notation for patterns as follows:
/// ```
/// addRule({callByNameOnly(name), deleteInstruction()});
/// ```
/// to delete the instructions that calls functions with the name `name`.
ReplacerFunction deleteInstruction();
/// @}

/// @addtogroup shorthandNotation
/// @{
/// Literals which ease the burned of capturing values and increase readibility of the code.

/// Literal for specifying the capture of a llvm::Value*. This literal calls the
/// IOperandPrototype::enableCapture through the assignment of a IOperandPrototypePtr to the class
/// Capture.
///
/// As an example, one may want to match the pattern `foo(bar(baz(x)), y)` and extract the variable
/// `x` to add meta data to it. The corresponding IR could look like:
/// ```
/// %1 = call %Type* @baz(%Type* %0)
/// %2 = call %Type* @bar(%Type* %1)
/// call void @foo(%Type* %2, %Type* %3)
/// ```
/// To match this pattern, one would create the pattern `call("foo", call("bar", call("baz", "x"_cap
/// = _)), _)`. This pattern would ensure that at the time where the replacer function is called,
/// the value stored in `%0` is captured under the name `x`.
///
Capture operator""_cap(char const *name, std::size_t);
/// @}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
