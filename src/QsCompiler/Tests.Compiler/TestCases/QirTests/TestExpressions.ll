entry:
  %0 = call %Callable* @Microsoft__Quantum__Testing__QIR__ReturnGlobalId__body()
  %1 = call %Callable* @Microsoft__Quantum__Testing__QIR__ReturnLocalId__body()
  %2 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnFunctionCall__body()
  %3 = call double @Microsoft__Quantum__Testing__QIR__ReturnOperationCall__body()
  %4 = call %Callable* @Microsoft__Quantum__Testing__QIR__ReturnPartialApplication__body()
  %5 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__ReturnUnwrapApplication__body()
  %6 = call %Callable* @Microsoft__Quantum__Testing__QIR__ReturnAdjointApplication__body()
  %7 = call %Callable* @Microsoft__Quantum__Testing__QIR__ReturnControlledApplication__body()
  %8 = call { i64, { %Callable*, %String* }* }* @Microsoft__Quantum__Testing__QIR__ReturnTuple__body()
  %9 = call %String* @Microsoft__Quantum__Testing__QIR__ReturnArrayItem__body()
  %10 = call i2 @Microsoft__Quantum__Testing__QIR__ReturnNamedItem__body()
  %11 = call %Array* @Microsoft__Quantum__Testing__QIR__ReturnArray__body()
  %12 = call %Array* @Microsoft__Quantum__Testing__QIR__ReturnNewArray__body()
  %13 = call %String* @Microsoft__Quantum__Testing__QIR__ReturnString__body()
  %14 = call %Range @Microsoft__Quantum__Testing__QIR__ReturnRange__body()
  %15 = call %Array* @Microsoft__Quantum__Testing__QIR__ReturnCopyAndUpdateArray__body()
  %16 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__ReturnCopyAndUpdateUdt__body()
  %17 = call %BigInt* @Microsoft__Quantum__Testing__QIR__ReturnConditional__body(i1 false)
  %18 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnEquality__body(i64 4, i64 5)
  %19 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnInequality__body(i64 5, i64 6)
  %20 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnLessThan__body(i64 5, i64 6)
  %21 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnLessThanOrEqual__body(i64 5, i64 6)
  %22 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnGreaterThan__body(i64 5, i64 6)
  %23 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnGreaterThanOrEqual__body(i64 5, i64 6)
  %24 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnLogicalAnd__body(i1 true, i1 false)
  %25 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnLogicalOr__body(i1 false, i1 true)
  %26 = call double @Microsoft__Quantum__Testing__QIR__ReturnAddition__body(double 1.000000e+00, double 2.000000e+00)
  %27 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnSubtraction__body(i64 3, i64 4)
  %28 = call double @Microsoft__Quantum__Testing__QIR__ReturnMultiplication__body(double 1.000000e+00, double 2.000000e+00)
  %29 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnDivision__body(i64 3, i64 4)
  %30 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnExponentiation1__body(i64 5)
  %31 = call double @Microsoft__Quantum__Testing__QIR__ReturnExponentiation2__body(double 6.000000e+00)
  %32 = call %BigInt* @Microsoft__Quantum__Testing__QIR__ReturnExponentiation3__body(i64 7)
  %33 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnModulo__body(i64 8)
  %34 = call i2 @Microsoft__Quantum__Testing__QIR__ReturnPauli__body()
  %35 = call %Result* @Microsoft__Quantum__Testing__QIR__ReturnResult__body()
  %36 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnBool__body()
  %37 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnInt__body()
  %38 = call double @Microsoft__Quantum__Testing__QIR__ReturnDouble__body()
  %39 = call %BigInt* @Microsoft__Quantum__Testing__QIR__ReturnBigInt__body()
  call void @Microsoft__Quantum__Testing__QIR__ReturnUnit__body()
  %40 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnLeftShift__body()
  %41 = call %BigInt* @Microsoft__Quantum__Testing__QIR__ReturnRightShift__body()
  %42 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnBXOr__body()
  %43 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnBOr__body()
  %44 = call %BigInt* @Microsoft__Quantum__Testing__QIR__ReturnBAnd__body()
  %45 = call i1 @Microsoft__Quantum__Testing__QIR__ReturnNot__body()
  %46 = call i64 @Microsoft__Quantum__Testing__QIR__ReturnBNot__body()
  %47 = call double @Microsoft__Quantum__Testing__QIR__ReturnNegative__body(double 3.000000e+00)
  %48 = getelementptr inbounds { { i2, i64 }*, double }, { { i2, i64 }*, double }* %5, i32 0, i32 0
  %49 = load { i2, i64 }*, { i2, i64 }** %48
  %50 = getelementptr inbounds { i64, { %Callable*, %String* }* }, { i64, { %Callable*, %String* }* }* %8, i32 0, i32 1
  %51 = load { %Callable*, %String* }*, { %Callable*, %String* }** %50
  %52 = getelementptr inbounds { %Callable*, %String* }, { %Callable*, %String* }* %51, i32 0, i32 0
  %53 = load %Callable*, %Callable** %52
  %54 = getelementptr inbounds { %Callable*, %String* }, { %Callable*, %String* }* %51, i32 0, i32 1
  %55 = load %String*, %String** %54
  %56 = getelementptr inbounds { { i2, i64 }*, double }, { { i2, i64 }*, double }* %16, i32 0, i32 0
  %57 = load { i2, i64 }*, { i2, i64 }** %56
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %0, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %0, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %1, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %1, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %4, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %4, i64 -1)
  %58 = bitcast { i2, i64 }* %49 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %58, i64 -1)
  %59 = bitcast { { i2, i64 }*, double }* %5 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %59, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %6, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %6, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %7, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %7, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %53, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %53, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %55, i64 -1)
  %60 = bitcast { %Callable*, %String* }* %51 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %60, i64 -1)
  %61 = bitcast { i64, { %Callable*, %String* }* }* %8 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %61, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  %62 = call i64 @__quantum__rt__array_get_size_1d(%Array* %11)
  %63 = sub i64 %62, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %64 = phi i64 [ 0, %entry ], [ %69, %exiting__1 ]
  %65 = icmp sle i64 %64, %63
  br i1 %65, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %66 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %11, i64 %64)
  %67 = bitcast i8* %66 to %String**
  %68 = load %String*, %String** %67
  call void @__quantum__rt__string_update_reference_count(%String* %68, i64 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %69 = add i64 %64, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %11, i64 -1)
  %70 = call i64 @__quantum__rt__array_get_size_1d(%Array* %12)
  %71 = sub i64 %70, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %72 = phi i64 [ 0, %exit__1 ], [ %77, %exiting__2 ]
  %73 = icmp sle i64 %72, %71
  br i1 %73, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %74 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 %72)
  %75 = bitcast i8* %74 to %Result**
  %76 = load %Result*, %Result** %75
  call void @__quantum__rt__result_update_reference_count(%Result* %76, i64 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %77 = add i64 %72, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %12, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %13, i64 -1)
  %78 = call i64 @__quantum__rt__array_get_size_1d(%Array* %15)
  %79 = sub i64 %78, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %80 = phi i64 [ 0, %exit__2 ], [ %85, %exiting__3 ]
  %81 = icmp sle i64 %80, %79
  br i1 %81, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %82 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %15, i64 %80)
  %83 = bitcast i8* %82 to %Result**
  %84 = load %Result*, %Result** %83
  call void @__quantum__rt__result_update_reference_count(%Result* %84, i64 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %85 = add i64 %80, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %15, i64 -1)
  %86 = bitcast { i2, i64 }* %57 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %86, i64 -1)
  %87 = bitcast { { i2, i64 }*, double }* %16 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %87, i64 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %17, i64 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %32, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %35, i64 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %39, i64 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %41, i64 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %44, i64 -1)
  ret void
}
