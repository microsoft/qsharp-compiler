// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/DefaultProfileGenerator.hpp"
#include "Llvm/Llvm.hpp"
#include "Rules/Factory.hpp"
#include "TestTools/IrManipulationTestHelper.hpp"
#include "gtest/gtest.h"

#include <functional>

using namespace microsoft::quantum;

namespace {
using IrManipulationTestHelperPtr = std::shared_ptr<IrManipulationTestHelper>;
IrManipulationTestHelperPtr newIrManip(std::string const &script)
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
  ir_manip->declareFunction(
      "%Result* @Microsoft__Quantum__Qir__Emission__Iterate__body(double, double, %Qubit*)");
  ir_manip->declareFunction("void @Microsoft__Quantum__Qir__Emission__Prepare__body(%Qubit*)");
  ir_manip->declareFunction("%Result* @__quantum__rt__result_get_zero()");

  if (!ir_manip->fromBodyString(script, "i64 %nrIter"))
  {
    llvm::errs() << ir_manip->generateScript(script, "i64 %nrIter") << "\n\n";
    llvm::errs() << ir_manip->getErrorMessage() << "\n";
    exit(-1);
  }
  return ir_manip;
}

}  // namespace

// Single allocation with action and then release
TEST(RuleTransformationPass, PhiElimination)
{
  auto ir_manip = newIrManip(R"script(
  %mu = alloca double, align 8
  store double 7.951000e-01, double* %mu, align 8
  %sigma = alloca double, align 8
  store double 6.065000e-01, double* %sigma, align 8
  %target = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @Microsoft__Quantum__Qir__Emission__Prepare__body(%Qubit* %target)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %0 = phi i64 [ 1, %entry ], [ %15, %exiting__1 ]
  %1 = icmp sle i64 %0, %nrIter
  br i1 %1, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %2 = load double, double* %mu, align 8
  %3 = call double @Microsoft__Quantum__Math__PI__body()
  %4 = load double, double* %sigma, align 8
  %5 = fmul double %3, %4
  %6 = fdiv double %5, 2.000000e+00
  %time = fsub double %2, %6
  %theta = fdiv double 1.000000e+00, %4
  %datum = call %Result* @Microsoft__Quantum__Qir__Emission__Iterate__body(double %time, double %theta, %Qubit* %target)
  %7 = call %Result* @__quantum__rt__result_get_zero()
  %8 = call i1 @__quantum__rt__result_equal(%Result* %datum, %Result* %7)
  br i1 %8, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %body__1
  %9 = fmul double %4, 6.065000e-01
  %10 = fsub double %2, %9
  br label %condContinue__1

condFalse__1:                                     ; preds = %body__1
  %11 = fmul double %4, 6.065000e-01
  %12 = fadd double %2, %11
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %13 = phi double [ %10, %condTrue__1 ], [ %12, %condFalse__1 ]
  store double %13, double* %mu, align 8
  %14 = fmul double %4, 7.951000e-01
  store double %14, double* %sigma, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %datum, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__1
  %15 = add i64 %0, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %16 = load double, double* %mu, align 8
  call void @__quantum__rt__qubit_release(%Qubit* %target)
  )script");

  auto profile = std::make_shared<DefaultProfileGenerator>();

  ConfigurationManager &configuration_manager = profile->configurationManager();
  configuration_manager.addConfig<FactoryConfiguration>();

  ir_manip->applyProfile(profile);
  llvm::errs() << *ir_manip->module() << "\n";
}
