define i1 @Microsoft__Quantum__Testing__QIR__TestControlled__body() #0 {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, i64 }* getelementptr ({ %TupleHeader, %Callable*, i64 }, { %TupleHeader, %Callable*, i64 }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, %Callable*, i64 }*
  %2 = getelementptr { %TupleHeader, %Callable*, i64 }, { %TupleHeader, %Callable*, i64 }* %1, i64 0, i32 1
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Intrinsic__K, %TupleHeader* null)
  store %Callable* %3, %Callable** %2
  %4 = getelementptr { %TupleHeader, %Callable*, i64 }, { %TupleHeader, %Callable*, i64 }* %1, i64 0, i32 2
  store i64 2, i64* %4
  %k2 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__1, %TupleHeader* %0)
  %ck2 = call %Callable* @__quantum__rt__callable_copy(%Callable* %k2)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ck2)
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %i = phi i64 [ 0, %preheader__1 ], [ %26, %exiting__1 ]
  %5 = icmp sge i64 %i, 100
  %6 = icmp sle i64 %i, 100
  %7 = select i1 true, i1 %6, i1 %5
  br i1 %7, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %ctrls = call %Array* @__quantum__rt__qubit_allocate_array(i64 2)
  %qb = call %Qubit* @__quantum__rt__qubit_allocate()
  %8 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Array*, %Qubit* }* getelementptr ({ %TupleHeader, %Array*, %Qubit* }, { %TupleHeader, %Array*, %Qubit* }* null, i32 1) to i64))
  %9 = bitcast %TupleHeader* %8 to { %TupleHeader, %Array*, %Qubit* }*
  %10 = getelementptr { %TupleHeader, %Array*, %Qubit* }, { %TupleHeader, %Array*, %Qubit* }* %9, i64 0, i32 1
  store %Array* %ctrls, %Array** %10
  %11 = getelementptr { %TupleHeader, %Array*, %Qubit* }, { %TupleHeader, %Array*, %Qubit* }* %9, i64 0, i32 2
  store %Qubit* %qb, %Qubit** %11
  call void @__quantum__rt__callable_invoke(%Callable* %ck2, %TupleHeader* %8, %TupleHeader* null)
  %moreCtrls = call %Array* @__quantum__rt__qubit_allocate_array(i64 3)
  %12 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Array*, { %TupleHeader, %Array*, %Qubit* }* }* getelementptr ({ %TupleHeader, %Array*, { %TupleHeader, %Array*, %Qubit* }* }, { %TupleHeader, %Array*, { %TupleHeader, %Array*, %Qubit* }* }* null, i32 1) to i64))
  %13 = bitcast %TupleHeader* %12 to { %TupleHeader, %Array*, { %TupleHeader, %Array*, %Qubit* }* }*
  %14 = getelementptr { %TupleHeader, %Array*, { %TupleHeader, %Array*, %Qubit* }* }, { %TupleHeader, %Array*, { %TupleHeader, %Array*, %Qubit* }* }* %13, i64 0, i32 1
  store %Array* %moreCtrls, %Array** %14
  %15 = call %TupleHeader* @__quantum__rt__tuple_create(i64 8)
  %16 = getelementptr { %TupleHeader, %Array*, { %TupleHeader, %Array*, %Qubit* }* }, { %TupleHeader, %Array*, { %TupleHeader, %Array*, %Qubit* }* }* %13, i64 0, i32 2
  %17 = bitcast %TupleHeader* %15 to { %TupleHeader, %Array*, %Qubit* }*
  store { %TupleHeader, %Array*, %Qubit* }* %17, { %TupleHeader, %Array*, %Qubit* }** %16
  %18 = bitcast %TupleHeader* %15 to { %TupleHeader, %Array*, %Qubit* }*
  %19 = getelementptr { %TupleHeader, %Array*, %Qubit* }, { %TupleHeader, %Array*, %Qubit* }* %18, i64 0, i32 1
  store %Array* %ctrls, %Array** %19
  %20 = getelementptr { %TupleHeader, %Array*, %Qubit* }, { %TupleHeader, %Array*, %Qubit* }* %18, i64 0, i32 2
  store %Qubit* %qb, %Qubit** %20
  %21 = call %Callable* @__quantum__rt__callable_copy(%Callable* %ck2)
  call void @__quantum__rt__callable_make_controlled(%Callable* %21)
  call void @__quantum__rt__callable_invoke(%Callable* %21, %TupleHeader* %12, %TupleHeader* null)
  call void @__quantum__rt__qubit_release_array(%Array* %moreCtrls)
  call void @__quantum__rt__callable_unreference(%Callable* %21)
  %22 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %23 = load %Result*, %Result** @ResultZero
  %24 = call i1 @__quantum__rt__result_equal(%Result* %22, %Result* %23)
  %25 = xor i1 %24, true
  br i1 %25, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  call void @__quantum__rt__qubit_release_array(%Array* %ctrls)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  call void @__quantum__rt__callable_unreference(%Callable* %k2)
  call void @__quantum__rt__callable_unreference(%Callable* %ck2)
  ret i1 false

continue__1:                                      ; preds = %body__1
  call void @__quantum__rt__qubit_release_array(%Array* %ctrls)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %26 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %k2)
  call void @__quantum__rt__callable_unreference(%Callable* %ck2)
  ret i1 true
}
