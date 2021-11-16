#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Logging/ILogger.hpp"
#include "Profile/Profile.hpp"
#include "QatTypes/QatTypes.hpp"
#include "Rules/RuleSet.hpp"
#include "TransformationRulesPass/TransformationRulesPassConfiguration.hpp"

#include "Llvm/Llvm.hpp"

#include <functional>
#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    /// This class applies a set of transformation rules to the IR to transform it into a new IR. The
    /// rules are added using the RuleSet class which allows the developer to create one or more rules
    /// on how to transform the IR.
    ///
    ///
    /// The module executes the following steps:
    ///           ┌─────────────────┐
    ///           │  Apply profile  │
    ///           └─────────────────┘
    ///                    │                ┌───────────────────────────────┐
    ///                    ├───────────────▶│   Copy and expand functions   │──┐
    ///                    │     clone      └───────────────────────────────┘  │
    ///                    │   functions?                   │ delete dead      │
    ///                    │                                ▼    code?         │
    ///                    │                ┌───────────────────────────────┐  │
    ///                    ├───────────────▶│     Determine active code     │  │
    ///                    │  delete dead   └───────────────────────────────┘  │
    ///                    │     code?                      │                  │ leave dead
    ///                    │                                ▼                  │    code?
    ///                    │                ┌───────────────────────────────┐  │
    ///                    │                │      Simplify phi nodes       │  │
    ///                    │                └───────────────────────────────┘  │
    ///                    │                                │                  │
    ///                    │                                ▼                  │
    ///                    │                ┌───────────────────────────────┐  │
    ///                    │                │       Delete dead code        │  │
    ///                    │                └───────────────────────────────┘  │
    ///                    │                                │                  │
    ///                    │                                ▼                  │
    ///                    │  fallback      ┌───────────────────────────────┐  │
    ///                    └───────────────▶│          Apply rules          │◀─┘
    ///                                     └───────────────────────────────┘
    ///
    /// Copying and expanding functions identifies function calls and identifies compile time constants
    /// passed to the function. It then copies the full implementation of the function, replacing all
    /// compile-time constants (and hence changing the function signature). That is, if a function call
    /// `f(x, 9)` is identified, it is replaced with `f.1(x)` where `f.1` is a copy of `f` with second
    /// argument written into the function.
    ///
    class TransformationRulesPass : public llvm::PassInfoMixin<TransformationRulesPass>
    {
      public:
        using Replacements         = ReplacementRule::Replacements;
        using Instruction          = llvm::Instruction;
        using Rules                = std::vector<ReplacementRule>;
        using Value                = llvm::Value;
        using Builder              = ReplacementRule::Builder;
        using AllocationManagerPtr = IAllocationManager::AllocationManagerPtr;
        using Captures             = RuleSet::Captures;
        using ConstantArguments    = std::unordered_map<std::string, llvm::ConstantInt*>;
        using ILoggerPtr           = std::shared_ptr<ILogger>;

        // Construction and destruction configuration.
        //

        /// Custom default constructor
        TransformationRulesPass(
            RuleSet&&                                   rule_set,
            TransformationRulesPassConfiguration const& config,
            Profile*                                    profile);

        /// Copy construction is banned.
        TransformationRulesPass(TransformationRulesPass const&) = delete;

        /// We allow move semantics.
        TransformationRulesPass(TransformationRulesPass&&) = default;

        /// Default destruction.
        ~TransformationRulesPass() = default;

        // Operators
        //

        /// Copy assignment is banned.
        TransformationRulesPass& operator=(TransformationRulesPass const&) = delete;

        /// Move assignment is permitted.
        TransformationRulesPass& operator=(TransformationRulesPass&&) = default;

        /// Implements the transformation analysis which uses the supplied ruleset to make substitutions
        /// in each function.
        llvm::PreservedAnalyses run(llvm::Module& module, llvm::ModuleAnalysisManager& mam);

        using DeletableInstructions = std::vector<llvm::Instruction*>;
        using InstructionModifier   = std::function<llvm::Value*(llvm::Value*, DeletableInstructions&)>;

        // Generic helper functions
        //

        /// Generic function to apply a instructionModifier (lambda function) to every instruction in the
        /// function `function`. This method follows the execution path to the extend possible and deals
        /// with branching if the branch statement can be evaluated at compile time.
        bool runOnFunction(llvm::Function& function, InstructionModifier const& modifier);

        /// Applies each of the replacements in the `replacements_` variable.
        void processReplacements();

        // Copy and expand
        //

        /// Configuration function for copy and expand to setup the necessary rules.
        void setupCopyAndExpand();

        /// Main function for running the copy and expand functionality. This function first identifies
        /// the entry point and then follows every execution path to copy the callee function for every
        /// call instruction encountered. This makes that every call in the code has its own unique callee
        /// function which ensures that when allocating qubits or results, the assigned registers are not
        /// accidentally reused.
        void runCopyAndExpand(llvm::Module& module, llvm::ModuleAnalysisManager& mam);

        /// Test whether the instruction is a call instruction and copy the callee in case it is. This
        /// function collects instructions which are scheduled for deletion at a later point.
        llvm::Value* copyAndExpand(llvm::Value* input, DeletableInstructions&);

        /// Copies the function body and replace function arguments whenever arguments are constant.
        llvm::Function* expandFunctionCall(llvm::Function& callee, ConstantArguments const& const_args = {});

        /// Folds all constant expression in function.
        void constantFoldFunction(llvm::Function& callee);

        /// Helper function to create replacements for constant expressions.
        void addConstExprRule(ReplacementRule&& rule);

        // Dead code detection
        //
        void         runDetectActiveCode(llvm::Module& module, llvm::ModuleAnalysisManager& mam);
        void         runDeleteDeadCode(llvm::Module& module, llvm::ModuleAnalysisManager& mam);
        llvm::Value* detectActiveCode(llvm::Value* input, DeletableInstructions&);
        llvm::Value* deleteDeadCode(llvm::Value* input, DeletableInstructions&);
        bool         isActive(llvm::Value* value) const;

        void followUsers(llvm::Value* value);

        // Phi replacement
        //

        /// Function which replaces phi nodes which refer to inactive blocks. That is, in cases where
        /// branch statement evaluates at compile time, only one block will be marked as active. For those
        /// case we can eliminate the phi nodes. In the case where branch statements cannot be evaluated
        /// all are marked as active. In this case, phi nodes are left unchanged.
        void runReplacePhi(llvm::Module& module, llvm::ModuleAnalysisManager& mam);

        // Rules
        //

        void runApplyRules(llvm::Module& module, llvm::ModuleAnalysisManager& mam);
        bool onQubitRelease(llvm::Instruction* instruction, Captures& captures);
        bool onQubitAllocate(llvm::Instruction* instruction, Captures& captures);

        /// Whether or not this pass is required to run.
        static bool isRequired();

        // Logger
        //
        void setLogger(ILoggerPtr logger);

      private:
        // Pass configuration
        //

        /// Rule set which describes a set of transformations to apply to the QIR.
        RuleSet rule_set_{};

        /// Configuration with enabled or disabled features, recursion limits etc.
        TransformationRulesPassConfiguration config_{};

        // Logging and data collection
        //

        /// Logger which is used to collect information, debug info, warnings, errors and internal errors.
        ILoggerPtr logger_{nullptr};

        // Execution path unrolling
        //

        /// Current recursion depth which is used to prevent unbound (at compile time) recursion.
        uint64_t depth_{0};

        // Copy and expand
        //

        /// Rule set which is used to collapse compile-time constant expressions.
        RuleSet const_expr_replacements_{};

        // Dead code elimination
        //

        /// Set to track active Values in the code.
        std::unordered_set<Value*> active_pieces_{};

        /// Vector with block pointers to delete
        std::vector<llvm::BasicBlock*> blocks_to_delete_{};

        /// Vector of function pointers to delete
        std::vector<llvm::Function*> functions_to_delete_{};

        // Phi detection
        //

        /// Registered replacements to be executed.
        Replacements replacements_;

        // Profile
        //

        /// Pointer to the current profile. This pointer is used to annotate top level functions with
        /// regards to how many qubits they require. TODO(tfr): Consider moving into its own component.
        Profile* profile_{nullptr};
    };

} // namespace quantum
} // namespace microsoft
