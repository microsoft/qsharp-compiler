// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/DefaultProfileGenerator.hpp"
#include "Rules/Factory.hpp"
#include "TestTools/IrManipulationTestHelper.hpp"
#include "gtest/gtest.h"

#include "Llvm/Llvm.hpp"

#include <functional>

using namespace microsoft::quantum;

namespace
{
using IrManipulationTestHelperPtr = std::shared_ptr<IrManipulationTestHelper>;
IrManipulationTestHelperPtr newIrManip(std::string const& script)
{
    IrManipulationTestHelperPtr ir_manip = std::make_shared<IrManipulationTestHelper>();

    ir_manip->declareOpaque("Qubit");
    ir_manip->declareOpaque("Result");
    ir_manip->declareOpaque("Array");
    ir_manip->declareOpaque("Tuple");
    ir_manip->declareOpaque("Range");
    ir_manip->declareOpaque("Callable");
    ir_manip->declareOpaque("String");

    ir_manip->declareFunction("%Qubit* @__quantum__rt__qubit_allocate()");
    ir_manip->declareFunction("void @__quantum__rt__qubit_release(%Qubit*)");
    ir_manip->declareFunction("void @__quantum__qis__h__body(%Qubit*)");

    ir_manip->declareFunction("%Array* @__quantum__rt__qubit_allocate_array(i64)");
    ir_manip->declareFunction("void @__quantum__rt__array_update_alias_count(%Array*, i32)");
    ir_manip->declareFunction("void @__quantum__qis__cnot__body(%Qubit*, %Qubit*)");
    ir_manip->declareFunction("i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64)");
    ir_manip->declareFunction("%Result* @__quantum__qis__m__body(%Qubit*)");
    ir_manip->declareFunction("void @__quantum__qis__reset__body(%Qubit*)");
    ir_manip->declareFunction("%Result* @__quantum__rt__result_get_one()");
    ir_manip->declareFunction("i1 @__quantum__rt__result_equal(%Result*, %Result*)");
    ir_manip->declareFunction("void @__quantum__rt__result_update_reference_count(%Result*, i32)");
    ir_manip->declareFunction("void @__quantum__qis__z__body(%Qubit*)");
    ir_manip->declareFunction("void @__quantum__qis__x__body(%Qubit*)");
    ir_manip->declareFunction("void @__quantum__rt__message(%String*)");
    ir_manip->declareFunction("void @__quantum__rt__qubit_release_array(%Array*)");
    ir_manip->declareFunction("%String* @__quantum__rt__result_to_string(%Result*)");
    ir_manip->declareFunction("void @__quantum__rt__string_update_reference_count(%String*, i32)");
    ir_manip->declareFunction("double @Microsoft__Quantum__Math__PI__body()");
    ir_manip->declareFunction("%Result* @Microsoft__Quantum__Qir__Emission__Iterate__body(double, double, %Qubit*)");
    ir_manip->declareFunction("void @Microsoft__Quantum__Qir__Emission__Prepare__body(%Qubit*)");
    ir_manip->declareFunction("void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit*, %Qubit*)");

    ir_manip->declareFunction("i64 @TeleportChain__Calculate__body(i64, %Qubit*)");
    ir_manip->declareFunction("void @Microsoft__Quantum__Intrinsic__H__body(%Qubit*)");

    if (!ir_manip->fromBodyString(script))
    {
        llvm::outs() << ir_manip->generateScript(script) << "\n\n";
        llvm::outs() << ir_manip->getErrorMessage() << "\n";
        exit(-1);
    }
    return ir_manip;
}

} // namespace

// Single allocation with action and then release
TEST(TransformationRulesPass, PhiEliminationBranch1)
{
    auto ir_manip = newIrManip(R"script(
  %c = inttoptr i64 0 to %Qubit*
  %n = add i64 0, 1
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %ret = alloca i64, align 8
  store i64 2, i64* %ret, align 4
  call void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %q)
  call void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* %c, %Qubit* %q)
  %0 = icmp ne i64 %n, 0
  br i1 %0, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  %1 = sub i64 %n, 1
  %2 = call i64 @TeleportChain__Calculate__body(i64 %1, %Qubit* %q)
  %3 = add i64 %2, 2
  store i64 %3, i64* %ret, align 4
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %entry
  %4 = load i64, i64* %ret, align 4
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  )script");

    auto profile = std::make_shared<DefaultProfileGenerator>();

    ConfigurationManager& configuration_manager = profile->configurationManager();
    configuration_manager.addConfig<FactoryConfiguration>();

    ir_manip->applyProfile(profile);
    EXPECT_TRUE(ir_manip->hasInstructionSequence({
        "tail call void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* null)",
        "tail call void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* null, %Qubit* null)",
        "%0 = tail call i64 @TeleportChain__Calculate__body(i64 0, %Qubit* null)",
    }));
}

TEST(TransformationRulesPass, PhiEliminationBranch0)
{
    auto ir_manip = newIrManip(R"script(
  %c = inttoptr i64 0 to %Qubit*    
  %n = add i64 0, 0
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %ret = alloca i64, align 8
  store i64 2, i64* %ret, align 4
  call void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %q)
  call void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* %c, %Qubit* %q)
  %0 = icmp ne i64 %n, 0
  br i1 %0, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  %1 = sub i64 %n, 1
  %2 = call i64 @TeleportChain__Calculate__body(i64 %1, %Qubit* %q)
  %3 = add i64 %2, 2
  store i64 %3, i64* %ret, align 4
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %entry
  %4 = load i64, i64* %ret, align 4
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  )script");

    auto profile = std::make_shared<DefaultProfileGenerator>();

    ConfigurationManager& configuration_manager = profile->configurationManager();
    configuration_manager.addConfig<FactoryConfiguration>();

    ir_manip->applyProfile(profile);

    EXPECT_TRUE(ir_manip->hasInstructionSequence({
        "tail call void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* null)",
        "tail call void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* null, %Qubit* null)",
    }));

    EXPECT_FALSE(ir_manip->hasInstructionSequence({
        "%0 = tail call i64 @TeleportChain__Calculate__body(i64 0, %Qubit* %q)",
    }));
}
