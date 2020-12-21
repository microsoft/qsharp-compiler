﻿define i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body() #0 {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, double }* getelementptr ({ %Callable*, double }, { %Callable*, double }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, double }*
  %2 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 0
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Rz, %Tuple* null)
  store %Callable* %3, %Callable** %2
  call void @__quantum__rt__callable_reference(%Callable* %3)
  %4 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 1
  store double 2.500000e-01, double* %4
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %0)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %i = phi i64 [ 0, %preheader__1 ], [ %24, %exiting__1 ]
  %5 = icmp sge i64 %i, 100
  %6 = icmp sle i64 %i, 100
  %7 = select i1 true, i1 %6, i1 %5
  br i1 %7, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %qb = call %Qubit* @__quantum__rt__qubit_allocate()
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %9 = bitcast %Tuple* %8 to { %Qubit* }*
  %10 = getelementptr { %Qubit* }, { %Qubit* }* %9, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %10
  %11 = bitcast { %Qubit* }* %9 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %11, %Tuple* null)
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %13 = bitcast %Tuple* %12 to { %Qubit* }*
  %14 = getelementptr { %Qubit* }, { %Qubit* }* %13, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %14
  %15 = bitcast { %Qubit* }* %13 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %Tuple* %15, %Tuple* null)
  %16 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %17 = load %Result*, %Result** @ResultZero
  %18 = call i1 @__quantum__rt__result_equal(%Result* %16, %Result* %17)
  %19 = xor i1 %18, true
  br i1 %19, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  %20 = bitcast { %Qubit* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
  %21 = bitcast { %Qubit* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %21)
  call void @__quantum__rt__result_unreference(%Result* %16)
  call void @__quantum__rt__result_unreference(%Result* %17)
  call void @__quantum__rt__callable_unreference(%Callable* %3)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 false

continue__1:                                      ; preds = %body__1
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  %22 = bitcast { %Qubit* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %22)
  %23 = bitcast { %Qubit* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %23)
  call void @__quantum__rt__result_unreference(%Result* %16)
  call void @__quantum__rt__result_unreference(%Result* %17)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %24 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %3)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}
