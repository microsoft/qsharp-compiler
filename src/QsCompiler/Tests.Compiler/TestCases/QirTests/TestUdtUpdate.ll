define { { double, i64 }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate__body(i64 %a, i64 %b) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, i64 }* getelementptr ({ double, i64 }, { double, i64 }* null, i32 1) to i64))
create tuple for argument in initial rhs construction

  %1 = bitcast %Tuple* %0 to { double, i64 }*
  %2 = getelementptr { double, i64 }, { double, i64 }* %1, i64 0, i32 0
  %3 = getelementptr { double, i64 }, { double, i64 }* %1, i64 0, i32 1
  store double 1.000000e+00, double* %2
  store i64 %a, i64* %3
  %4 = call { { double, i64 }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestType__body({ double, i64 }* %1, i64 %b)
evaluate initial rhs

  %x = alloca { { double, i64 }*, i64 }*
  store { { double, i64 }*, i64 }* %4, { { double, i64 }*, i64 }** %x
  %5 = load { { double, i64 }*, i64 }*, { { double, i64 }*, i64 }** %x // FIXME: unnecessary load
  %6 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %5, i64 0, i32 0
  %7 = load { double, i64 }*, { double, i64 }** %6
  %8 = bitcast { double, i64 }* %7 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %8)
  %9 = bitcast { { double, i64 }*, i64 }* %5 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %9)
assign to x and increase access count for orig outer and orig inner tuple

  %10 = load { { double, i64 }*, i64 }*, { { double, i64 }*, i64 }** %x
  %11 = bitcast { { double, i64 }*, i64 }* %10 to %Tuple*
  %12 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %11, i1 false)
  %13 = bitcast %Tuple* %12 to { { double, i64 }*, i64 }*
  %14 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %13, i64 0, i32 0
  %15 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %13, i64 0, i32 1
  %16 = load i64, i64* %15 // FIXME: SPARE LOAD (FIXED BY RETURNING POINTERVALUES)
create new outer tuple and increase the ref count for everything that is not updated

  %17 = load { double, i64 }*, { double, i64 }** %14
  %18 = bitcast { double, i64 }* %17 to %Tuple*
  %19 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %18, i1 false)
  %20 = bitcast %Tuple* %19 to { double, i64 }*
  store { double, i64 }* %20, { double, i64 }** %14
create new inner tuple and store it in the outer copy

  %21 = getelementptr { double, i64 }, { double, i64 }* %20, i64 0, i32 0
  %22 = getelementptr { double, i64 }, { double, i64 }* %20, i64 0, i32 1
  %23 = load double, double* %21
  store i64 2, i64* %22
update the ref count in the inner tuple items for everything that is not updated, and update the item

  %24 = load { { double, i64 }*, i64 }*, { { double, i64 }*, i64 }** %x
  %25 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %24, i64 0, i32 0
  %26 = load { double, i64 }*, { double, i64 }** %25
  %27 = bitcast { double, i64 }* %26 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %27)
  %28 = bitcast { { double, i64 }*, i64 }* %24 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %28)
decrease the access count for the original outer and inner tuple

  store { { double, i64 }*, i64 }* %13, { { double, i64 }*, i64 }** %x
  %29 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %13, i64 0, i32 0
  %30 = load { double, i64 }*, { double, i64 }** %29
  %31 = bitcast { double, i64 }* %30 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %31)
  call void @__quantum__rt__tuple_add_access(%Tuple* %12)
assign the constructed copy and update to x and increase the access count for outer and inner 

  %32 = load { { double, i64 }*, i64 }*, { { double, i64 }*, i64 }** %x
  %33 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %32, i64 0, i32 0
  %34 = load { double, i64 }*, { double, i64 }** %33
  %35 = bitcast { double, i64 }* %34 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %35)
  %36 = bitcast { { double, i64 }*, i64 }* %32 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %36)
reference the outer and inner tuple of the copy and update, because it will be returned

  %37 = load { { double, i64 }*, i64 }*, { { double, i64 }*, i64 }** %x
  %38 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %37, i64 0, i32 0
  %39 = load { double, i64 }*, { double, i64 }** %38
  %40 = bitcast { double, i64 }* %39 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %40)
  %41 = bitcast { { double, i64 }*, i64 }* %37 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %41)
remove access for the outer and inner tuple in copy and update (x is going out of scope)
  
  call void @__quantum__rt__tuple_unreference(%Tuple* %0)
unref the first tuple that was the arg for the original construction (will be dealloc)

  %42 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %4, i64 0, i32 0
  %43 = load { double, i64 }*, { double, i64 }** %42
  %44 = bitcast { double, i64 }* %43 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %44)
  %45 = bitcast { { double, i64 }*, i64 }* %4 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %45)
unreference the orig outer and inner tuple (will be dealloc)

  %46 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %13, i64 0, i32 0
  %47 = load { double, i64 }*, { double, i64 }** %46
  %48 = bitcast { double, i64 }* %47 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %48)
  call void @__quantum__rt__tuple_unreference(%Tuple* %12)
unreference the copy outer and inner tuple (remains alive with ref count 1)

  call void @__quantum__rt__tuple_unreference(%Tuple* %19)
unreference created inner tuple

  ret { { double, i64 }*, i64 }* %32
}
