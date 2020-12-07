define i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body() #0 {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, double }* getelementptr ({ %TupleHeader, %Callable*, double }, { %TupleHeader, %Callable*, double }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, %Callable*, double }*
  %2 = getelementptr { %TupleHeader, %Callable*, double }, { %TupleHeader, %Callable*, double }* %1, i64 0, i32 1
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Intrinsic__Rz, %TupleHeader* null)
  store %Callable* %3, %Callable** %2
  %4 = getelementptr { %TupleHeader, %Callable*, double }, { %TupleHeader, %Callable*, double }* %1, i64 0, i32 2
  store double 2.500000e-01, double* %4
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__1, %TupleHeader* %0)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %i = phi i64 [ 0, %preheader__1 ], [ %18, %exiting__1 ]
  %5 = icmp sge i64 %i, 100
  %6 = icmp sle i64 %i, 100
  %7 = select i1 true, i1 %6, i1 %5
  br i1 %7, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %qb = call %Qubit* @__quantum__rt__qubit_allocate()
  %8 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Qubit* }* getelementptr ({ %TupleHeader, %Qubit* }, { %TupleHeader, %Qubit* }* null, i32 1) to i64))
  %9 = bitcast %TupleHeader* %8 to { %TupleHeader, %Qubit* }*
  %10 = getelementptr { %TupleHeader, %Qubit* }, { %TupleHeader, %Qubit* }* %9, i64 0, i32 1
  store %Qubit* %qb, %Qubit** %10
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %TupleHeader* %8, %TupleHeader* null)
  %11 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Qubit* }* getelementptr ({ %TupleHeader, %Qubit* }, { %TupleHeader, %Qubit* }* null, i32 1) to i64))
  %12 = bitcast %TupleHeader* %11 to { %TupleHeader, %Qubit* }*
  %13 = getelementptr { %TupleHeader, %Qubit* }, { %TupleHeader, %Qubit* }* %12, i64 0, i32 1
  store %Qubit* %qb, %Qubit** %13
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %TupleHeader* %11, %TupleHeader* null)
  %14 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %15 = load %Result*, %Result** @ResultZero
  %16 = call i1 @__quantum__rt__result_equal(%Result* %14, %Result* %15)
  %17 = xor i1 %16, true
  br i1 %17, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 false

continue__1:                                      ; preds = %body__1
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %18 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}
