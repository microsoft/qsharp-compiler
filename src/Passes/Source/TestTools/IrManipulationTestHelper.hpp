#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/ProfileGenerator.hpp"

#include "Llvm/Llvm.hpp"

#include <unordered_set>
#include <vector>

namespace microsoft
{
namespace quantum
{

    class IrManipulationTestHelper
    {
      public:
        using String            = std::string;
        using LLVMContext       = llvm::LLVMContext;
        using SMDiagnostic      = llvm::SMDiagnostic;
        using Module            = llvm::Module;
        using ModulePtr         = std::unique_ptr<Module>;
        using Strings           = std::vector<String>;
        using OptimizationLevel = llvm::PassBuilder::OptimizationLevel;
        using GeneratorPtr      = std::shared_ptr<ProfileGenerator>;

        // IrManipulationTestHelper is default constructible with no ability to move
        // or copy.
        //

        IrManipulationTestHelper();
        IrManipulationTestHelper(IrManipulationTestHelper const&) = delete;
        IrManipulationTestHelper& operator=(IrManipulationTestHelper const&) = delete;
        IrManipulationTestHelper(IrManipulationTestHelper&&)                 = delete;
        IrManipulationTestHelper& operator=(IrManipulationTestHelper&&) = delete;

        // Output functions
        //

        /// Generates a string for the IR currently held in the module.
        String toString() const;

        /// Generates a list of instructions for the main function in the module.
        Strings toBodyInstructions();

        // Test functions
        //

        /// Tests whether the main body contains a sequence of instructions. This function
        /// ignores instructions in-between the instruction set given.
        bool hasInstructionSequence(Strings const& instructions);

        /// Applies a profile to the module to allow which transforms the IR. This
        /// allow us to write small profiles to test a single piece of transformation.
        void applyProfile(
            GeneratorPtr const&      profile,
            OptimizationLevel const& optimisation_level = OptimizationLevel::O0,
            bool                     debug              = false);

        // Declaration of partial or full IR
        //

        /// Declares a opaque type. Only the name of the type should be supplied to
        /// this function. Example usage
        ///
        /// ```
        /// irmanip.declareOpaque("Qubit");
        /// ```
        void declareOpaque(String const& name);

        /// Declares a function. The full signature should be supplied to
        /// as the first argument. Example usage
        ///
        /// ```
        /// irmanip.declareOpaque("%Result* @__quantum__rt__result_get_zero()");
        /// ```
        void declareFunction(String const& declaration);

        /// Creates an LLVM module given a function body. This function makes use
        /// of the inputs from IrManipulationTestHelper::declareOpaque and
        /// IrManipulationTestHelper::declareFunction to construct the full
        /// IR. Example usage:
        ///
        /// ```
        /// irmanip.fromBodyString(R"script(
        /// %leftMessage = call %Qubit* @__quantum__rt__qubit_allocate()
        /// call void @__quantum__qis__h__body(%Qubit* %leftMessage)
        /// )script");
        /// ```
        ///
        /// Returns false if the IR is invalid.
        bool fromBodyString(String const& body, String const& args = "");

        /// Generates a script given the body of the main function.
        String generateScript(String const& body, String const& args = "") const;

        /// Creates an LLVM module given from a fully specified IR. This function
        /// ignores all inputs from IrManipulationTestHelper::declareOpaque and
        /// IrManipulationTestHelper::declareFunction.
        ///
        /// Returns false if the IR is invalid.
        bool fromString(String const& data);

        /// Gets an error message if the compilation failed.
        String getErrorMessage() const;

        /// Whether or not the module is broken.
        bool isModuleBroken();

        // Acccess member functions
        //

        /// Returns a reference to the module
        ModulePtr& module();

      private:
        // Declarations
        //

        /// Set of opaque type declarations
        std::unordered_set<std::string> opaque_declarations_{};

        /// Set of function declarations
        std::unordered_set<std::string> function_declarations_{};

        // Compilation state
        //

        /// Whether the compilation failed.
        bool compilation_failed_{false};

        /// The LLVM error encountered.
        SMDiagnostic error_;

        /// The LLVM context.
        LLVMContext context_;

        /// Pointer to the module obtained from the compilation process.
        ModulePtr module_;

        // Objects used to run a set of passes
        //
        llvm::PassBuilder             pass_builder_;
        llvm::LoopAnalysisManager     loop_analysis_manager_;
        llvm::FunctionAnalysisManager function_analysis_manager_;
        llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
        llvm::ModuleAnalysisManager   module_analysis_manager_;
    };

} // namespace quantum
} // namespace microsoft
