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
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  call void @__quantum__rt__callable_unreference(%Callable* %1)
  call void @__quantum__rt__callable_unreference(%Callable* %4)
  %48 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %5, i64 0, i32 0
  %49 = load { i2, i64 }*, { i2, i64 }** %48
  %50 = bitcast { i2, i64 }* %49 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %50)
  %51 = bitcast { { i2, i64 }*, double }* %5 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %51)
  call void @__quantum__rt__callable_unreference(%Callable* %6)
  call void @__quantum__rt__callable_unreference(%Callable* %7)
  %52 = getelementptr { i64, { %Callable*, %String* }* }, { i64, { %Callable*, %String* }* }* %8, i64 0, i32 1
  %53 = load { %Callable*, %String* }*, { %Callable*, %String* }** %52
  %54 = getelementptr { %Callable*, %String* }, { %Callable*, %String* }* %53, i64 0, i32 0
  %55 = load %Callable*, %Callable** %54
  call void @__quantum__rt__callable_unreference(%Callable* %55)
  %56 = getelementptr { %Callable*, %String* }, { %Callable*, %String* }* %53, i64 0, i32 1
  %57 = load %String*, %String** %56
  call void @__quantum__rt__string_unreference(%String* %57)
  %58 = bitcast { %Callable*, %String* }* %53 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %58)
  %59 = bitcast { i64, { %Callable*, %String* }* }* %8 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %59)
  call void @__quantum__rt__string_unreference(%String* %9)
  %60 = call i64 @__quantum__rt__array_get_size_1d(%Array* %11)
  %61 = sub i64 %60, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %62 = phi i64 [ 0, %entry ], [ %67, %exiting__1 ]
  %63 = icmp sle i64 %62, %61
  br i1 %63, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %64 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %11, i64 %62)
  %65 = bitcast i8* %64 to %String**
  %66 = load %String*, %String** %65
  call void @__quantum__rt__string_unreference(%String* %66)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %67 = add i64 %62, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_unreference(%Array* %11)
  %68 = call i64 @__quantum__rt__array_get_size_1d(%Array* %12)
  %69 = sub i64 %68, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %70 = phi i64 [ 0, %exit__1 ], [ %75, %exiting__2 ]
  %71 = icmp sle i64 %70, %69
  br i1 %71, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %72 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 %70)
  %73 = bitcast i8* %72 to %Result**
  %74 = load %Result*, %Result** %73
  call void @__quantum__rt__result_unreference(%Result* %74)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %75 = add i64 %70, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_unreference(%Array* %12)
  call void @__quantum__rt__string_unreference(%String* %13)
  %76 = call i64 @__quantum__rt__array_get_size_1d(%Array* %15)
  %77 = sub i64 %76, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %78 = phi i64 [ 0, %exit__2 ], [ %83, %exiting__3 ]
  %79 = icmp sle i64 %78, %77
  br i1 %79, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %80 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %15, i64 %78)
  %81 = bitcast i8* %80 to %Result**
  %82 = load %Result*, %Result** %81
  call void @__quantum__rt__result_unreference(%Result* %82)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %83 = add i64 %78, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_unreference(%Array* %15)
  %84 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %16, i64 0, i32 0
  %85 = load { i2, i64 }*, { i2, i64 }** %84
  %86 = bitcast { i2, i64 }* %85 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %86)
  %87 = bitcast { { i2, i64 }*, double }* %16 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %87)
  call void @__quantum__rt__bigint_unreference(%BigInt* %17)
  call void @__quantum__rt__bigint_unreference(%BigInt* %32)
  call void @__quantum__rt__result_unreference(%Result* %35)
  call void @__quantum__rt__bigint_unreference(%BigInt* %39)
  call void @__quantum__rt__bigint_unreference(%BigInt* %41)
  call void @__quantum__rt__bigint_unreference(%BigInt* %44)
  ret void
}
