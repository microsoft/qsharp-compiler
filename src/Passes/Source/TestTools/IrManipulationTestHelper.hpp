#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Profiles/IProfile.hpp"

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
        using ProfilePtr        = std::shared_ptr<IProfile>;

        /// IrManipulationTestHelper is default constructible with no ability to move
        /// or copy.
        /// @{
        IrManipulationTestHelper();
        IrManipulationTestHelper(IrManipulationTestHelper const&) = delete;
        IrManipulationTestHelper& operator=(IrManipulationTestHelper const&) = delete;
        IrManipulationTestHelper(IrManipulationTestHelper&&)                 = delete;
        IrManipulationTestHelper& operator=(IrManipulationTestHelper&&) = delete;
        /// @}

        /// Output functions
        /// @{

        /// Generates a string for the IR currently held in the module.
        String toString() const;

        /// Generates a list of instructions for the main function in the module.
        Strings toBodyInstructions() const;
        /// @}

        /// Test functions
        /// @{

        /// Tests whether the main body contains a sequence of instructions.
        bool hasInstructionSequence(Strings const& instructions);
        void applyProfile(
            ProfilePtr const&        profile,
            OptimizationLevel const& optimisation_level = OptimizationLevel::O0,
            bool                     debug              = false);
        /// @}

        /// Declaration of partial or full IR
        /// @{

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
        /// call void @__quantum__qis__h(%Qubit* %leftMessage)
        /// )script");
        /// ```
        void fromBodyString(String const& body);

        /// Creates an LLVM module given from a fully specified IR. This function
        /// ignores all inputs from IrManipulationTestHelper::declareOpaque and
        /// IrManipulationTestHelper::declareFunction.
        void fromString(String const& data);

        /// @}

        /// Acccess member functions
        /// @{
        llvm::PassBuilder&             passBuilder();
        llvm::LoopAnalysisManager&     loopAnalysisManager();
        llvm::FunctionAnalysisManager& functionAnalysisManager();
        llvm::CGSCCAnalysisManager&    gsccAnalysisManager();
        llvm::ModuleAnalysisManager&   moduleAnalysisManager();
        ModulePtr&                     module();
        /// @}
      private:
        std::unordered_set<std::string> opaque_declarations_{};
        std::unordered_set<std::string> function_declarations_{};

        /// @{
        SMDiagnostic error_;
        LLVMContext  context_;
        ModulePtr    module_;
        /// @}

        /// Objects used to run a set of passes
        /// @{
        llvm::PassBuilder             pass_builder_;
        llvm::LoopAnalysisManager     loop_analysis_manager_;
        llvm::FunctionAnalysisManager function_analysis_manager_;
        llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
        llvm::ModuleAnalysisManager   module_analysis_manager_;
        /// @}
    };

} // namespace quantum
} // namespace microsoft
