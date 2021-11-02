// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "TestTools/IrManipulationTestHelper.hpp"
#include "gtest/gtest.h"

using namespace microsoft::quantum;
TEST(TestToolsTestSuite, IrParitalConstruction)
{

    IrManipulationTestHelper input;

    input.declareOpaque("Qubit");
    input.declareOpaque("Result");

    input.declareFunction("i1 @__quantum__rt__result_equal(%Result*, %Result*)");
    input.declareFunction("%Qubit* @__quantum__rt__qubit_allocate()");
    input.declareFunction("void @__quantum__rt__qubit_release(%Qubit*)");
    input.declareFunction("void @__quantum__qis__h__body(%Qubit*)");
    input.declareFunction("%Result* @__quantum__rt__result_get_zero()");
    input.declareFunction("void @__quantum__qis__mz__body(%Qubit*, %Result*)");

    input.fromBodyString(R"script(
  %leftMessage = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__body(%Qubit* %leftMessage)
  call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)

  %0 = call %Result* @__quantum__rt__result_get_zero()
  %1 = call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 3 to %Result*), %Result* %0)

  ret i8 0
  )script");

    if (input.isModuleBroken())
    {
        llvm::outs() << input.getErrorMessage() << "\n";
        exit(-1);
    }

    EXPECT_TRUE(input.hasInstructionSequence({}));
    EXPECT_TRUE(input.hasInstructionSequence(
        {"call void @__quantum__qis__h__body(%Qubit* %leftMessage)",
         "%0 = call %Result* @__quantum__rt__result_get_zero()"}));

    EXPECT_TRUE(input.hasInstructionSequence(
        {"%0 = call %Result* @__quantum__rt__result_get_zero()",
         "%1 = call i1 @__quantum__rt__result_equal(%Result* nonnull "
         "inttoptr (i64 3 to %Result*), %Result* %0)"}));

    EXPECT_FALSE(input.hasInstructionSequence(
        {"%0 = call %Result* @__quantum__rt__result_get_zero()",
         "call void @__quantum__qis__h__body(%Qubit* %leftMessage)"}));

    EXPECT_FALSE(input.hasInstructionSequence({"%0 = call %Result* @non_existant_function()"}));
    EXPECT_FALSE(input.hasInstructionSequence({""}));
}

TEST(TestToolsTestSuite, IrFullConstruction)
{

    IrManipulationTestHelper input;

    input.fromString(R"script(
; ModuleID = 'IrManipulationTestHelper'
source_filename = "IrManipulationTestHelper.ll"

%Qubit = type opaque
%Result = type opaque

define i8 @Main() local_unnamed_addr {
entry:
  %leftMessage = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__body(%Qubit* %leftMessage)
  call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)
  %0 = call %Result* @__quantum__rt__result_get_zero()
  %1 = call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 3 to %Result*), %Result* %0)
  ret i8 0
}

declare void @__quantum__qis__mz__body(%Qubit*, %Result*) local_unnamed_addr

declare %Result* @__quantum__rt__result_get_zero() local_unnamed_addr

declare void @__quantum__qis__h__body(%Qubit*) local_unnamed_addr

declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

declare i1 @__quantum__rt__result_equal(%Result*, %Result*) local_unnamed_addr

  )script");

    EXPECT_TRUE(input.hasInstructionSequence({}));
    EXPECT_TRUE(input.hasInstructionSequence(
        {"call void @__quantum__qis__h__body(%Qubit* %leftMessage)",
         "%0 = call %Result* @__quantum__rt__result_get_zero()"}));

    EXPECT_TRUE(input.hasInstructionSequence(
        {"%0 = call %Result* @__quantum__rt__result_get_zero()",
         "%1 = call i1 @__quantum__rt__result_equal(%Result* nonnull "
         "inttoptr (i64 3 to %Result*), %Result* %0)"}));

    EXPECT_FALSE(input.hasInstructionSequence(
        {"%0 = call %Result* @__quantum__rt__result_get_zero()",
         "call void @__quantum__qis__h__body(%Qubit* %leftMessage)"}));

    EXPECT_FALSE(input.hasInstructionSequence({"%0 = call %Result* @non_existant_function()"}));
    EXPECT_FALSE(input.hasInstructionSequence({""}));
}

TEST(TestToolsTestSuite, ErrorOutput)
{

    IrManipulationTestHelper input;

    input.fromString(R"script(
; ModuleID = 'IrManipulationTestHelper'
source_filename = "IrManipulationTestHelper.ll"

define i8 @Main() local_unnamed_addr {
entry:
  %0 = call i1 @__call_to_unkown()
  ret i8 0
}
  )script");

    EXPECT_TRUE(input.isModuleBroken());
    EXPECT_TRUE(
        input.getErrorMessage().find("Error at 7:15: use of undefined value '@__call_to_unkown'") != std::string::npos);
}

TEST(TestToolsTestSuite, BrokenIRFunctions)
{
    using Strings = IrManipulationTestHelper::Strings;

    {
        IrManipulationTestHelper input;

        input.fromString(R"script(
; ModuleID = 'IrManipulationTestHelper'
source_filename = "IrManipulationTestHelper.ll"

%Qubit = type opaque
%Result = type opaque

define i8 @Main2() local_unnamed_addr {
entry:
  %leftMessage = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__body(%Qubit* %leftMessage)
  call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)
  %0 = call %Result* @__quantum__rt__result_get_zero()
  %1 = call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 3 to %Result*), %Result* %0)
  ret i8 0
}

declare void @__quantum__qis__mz__body(%Qubit*, %Result*) local_unnamed_addr

declare %Result* @__quantum__rt__result_get_zero() local_unnamed_addr

declare void @__quantum__qis__h__body(%Qubit*) local_unnamed_addr

declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

declare i1 @__quantum__rt__result_equal(%Result*, %Result*) local_unnamed_addr

  )script");

        EXPECT_EQ(input.toBodyInstructions(), Strings({}));
    }

    {
        IrManipulationTestHelper input;

        input.fromString(R"script(
  ; ModuleID = 'IrManipulationTestHelper'
  source_filename = "IrManipulationTestHelper.ll"

  %Qubit = type opaque
  %Result = type opaque

  define i8 @Main() local_unnamed_addr {
  entry2:
    %leftMessage = call %Qubit* @__quantum__rt__qubit_allocate()
    call void @__quantum__qis__h__body(%Qubit* %leftMessage)
    call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)
    %0 = call %Result* @__quantum__rt__result_get_zero()
    %1 = call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 3 to %Result*),
  %Result* %0) ret i8 0
  }

  declare void @__quantum__qis__mz__body(%Qubit*, %Result*) local_unnamed_addr

  declare %Result* @__quantum__rt__result_get_zero() local_unnamed_addr

  declare void @__quantum__qis__h__body(%Qubit*) local_unnamed_addr

  declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

  declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

  declare i1 @__quantum__rt__result_equal(%Result*, %Result*) local_unnamed_addr

    )script");

        EXPECT_EQ(input.toBodyInstructions(), Strings({}));
    }

    {
        IrManipulationTestHelper input;

        input.fromString(R"script(
  ; ModuleID = 'IrManipulationTestHelper'
  source_filename = "IrManipulationTestHelper.ll"

  %Qubit = type opaque
  %Result = type opaque

  define i8 @Main() local_unnamed_addr {
  entry2:
    %leftMessage = call %Qubit* @__quantum__rt__qubit_allocate()
    call void @__unknown_function(%Qubit* %leftMessage)
    call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)
    %0 = call %Result* @__quantum__rt__result_get_zero()
    %1 = call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 3 to %Result*),
  %Result* %0) ret i8 0
  }

  declare void @__quantum__qis__mz__body(%Qubit*, %Result*) local_unnamed_addr

  declare %Result* @__quantum__rt__result_get_zero() local_unnamed_addr

  declare void @__quantum__qis__h__body(%Qubit*) local_unnamed_addr

  declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

  declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

  declare i1 @__quantum__rt__result_equal(%Result*, %Result*) local_unnamed_addr

    )script");

        EXPECT_EQ(input.toBodyInstructions(), Strings({}));
    }
}
