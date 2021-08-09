; ModuleID = 'qir/ConstSizeArray.ll'
source_filename = "qir/ConstSizeArray.ll"

%Tuple = type opaque
%Qubit = type opaque
%Array = type opaque
%Result = type opaque
%Callable = type opaque
%String = type opaque

@Microsoft__Quantum__Qir__Emission__M = internal constant [4 x void (%Tuple*, %Tuple*, %Tuple*)*] [void (%Tuple*, %Tuple*, %Tuple*)* @Microsoft__Quantum__Qir__Emission__M__body__wrapper, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null]
@0 = internal constant [3 x i8] c", \00"
@1 = internal constant [2 x i8] c"[\00"
@2 = internal constant [2 x i8] c"]\00"

declare void @__quantum__qis__cnot__body(%Qubit*, %Qubit*) local_unnamed_addr

declare void @__quantum__qis__cnot__adj(%Qubit*, %Qubit*) local_unnamed_addr

declare void @__quantum__rt__array_update_alias_count(%Array*, i32) local_unnamed_addr

declare %Tuple* @__quantum__rt__tuple_create(i64) local_unnamed_addr

declare void @__quantum__rt__tuple_update_reference_count(%Tuple*, i32) local_unnamed_addr

define internal fastcc %Result* @Microsoft__Quantum__Qir__Emission__M__body(%Qubit* %q) unnamed_addr {
entry:
  %0 = call %Result* @__quantum__qis__m__body(%Qubit* %q)
  ret %Result* %0
}

declare %Result* @__quantum__qis__m__body(%Qubit*) local_unnamed_addr

define internal fastcc void @Microsoft__Quantum__Qir__Emission__Majority__body(%Qubit* %a, %Qubit* %b, %Qubit* %c) unnamed_addr {
entry:
  call void @__quantum__qis__cnot__body(%Qubit* %c, %Qubit* %b)
  call void @__quantum__qis__cnot__body(%Qubit* %c, %Qubit* %a)
  call void @__quantum__qis__toffoli__body(%Qubit* %a, %Qubit* %b, %Qubit* %c)
  ret void
}

declare void @__quantum__qis__toffoli__body(%Qubit*, %Qubit*, %Qubit*) local_unnamed_addr

define internal fastcc void @Microsoft__Quantum__Qir__Emission__Majority__adj(%Qubit* %a, %Qubit* %b, %Qubit* %c) unnamed_addr {
entry:
  call void @__quantum__qis__toffoli__adj(%Qubit* %a, %Qubit* %b, %Qubit* %c)
  call void @__quantum__qis__cnot__adj(%Qubit* %c, %Qubit* %a)
  call void @__quantum__qis__cnot__adj(%Qubit* %c, %Qubit* %b)
  ret void
}

declare void @__quantum__qis__toffoli__adj(%Qubit*, %Qubit*, %Qubit*) local_unnamed_addr

define internal fastcc %Array* @Microsoft__Quantum__Qir__Emission__RunAdder__body() unnamed_addr {
entry:
  %a = call %Array* @__quantum__rt__qubit_allocate_array(i64 4)
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i32 1)
  %b = call %Array* @__quantum__rt__qubit_allocate_array(i64 4)
  call void @__quantum__rt__array_update_alias_count(%Array* %b, i32 1)
  %cin = call %Qubit* @__quantum__rt__qubit_allocate()
  %cout = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 0)
  %1 = bitcast i8* %0 to %Qubit**
  %q = load %Qubit*, %Qubit** %1, align 8
  call void @__quantum__qis__x__body(%Qubit* %q)
  %2 = call i64 @__quantum__rt__array_get_size_1d(%Array* %b)
  %3 = add i64 %2, -1
  %.not1 = icmp slt i64 %3, 0
  br i1 %.not1, label %exit__1, label %body__1

body__1:                                          ; preds = %entry, %body__1
  %4 = phi i64 [ %7, %body__1 ], [ 0, %entry ]
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 %4)
  %6 = bitcast i8* %5 to %Qubit**
  %q__1 = load %Qubit*, %Qubit** %6, align 8
  call void @__quantum__qis__x__body(%Qubit* %q__1)
  %7 = add i64 %4, 1
  %.not = icmp sgt i64 %7, %3
  br i1 %.not, label %exit__1, label %body__1

exit__1:                                          ; preds = %body__1, %entry
  %8 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 0)
  %9 = bitcast i8* %8 to %Qubit**
  %10 = load %Qubit*, %Qubit** %9, align 8
  %11 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 0)
  %12 = bitcast i8* %11 to %Qubit**
  %13 = load %Qubit*, %Qubit** %12, align 8
  call fastcc void @Microsoft__Quantum__Qir__Emission__Majority__body(%Qubit* %cin, %Qubit* %10, %Qubit* %13)
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 0)
  %15 = bitcast i8* %14 to %Qubit**
  %16 = load %Qubit*, %Qubit** %15, align 8
  %17 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 1)
  %18 = bitcast i8* %17 to %Qubit**
  %19 = load %Qubit*, %Qubit** %18, align 8
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 1)
  %21 = bitcast i8* %20 to %Qubit**
  %22 = load %Qubit*, %Qubit** %21, align 8
  call fastcc void @Microsoft__Quantum__Qir__Emission__Majority__body(%Qubit* %16, %Qubit* %19, %Qubit* %22)
  %23 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 1)
  %24 = bitcast i8* %23 to %Qubit**
  %25 = load %Qubit*, %Qubit** %24, align 8
  %26 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 2)
  %27 = bitcast i8* %26 to %Qubit**
  %28 = load %Qubit*, %Qubit** %27, align 8
  %29 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 2)
  %30 = bitcast i8* %29 to %Qubit**
  %31 = load %Qubit*, %Qubit** %30, align 8
  call fastcc void @Microsoft__Quantum__Qir__Emission__Majority__body(%Qubit* %25, %Qubit* %28, %Qubit* %31)
  %32 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 2)
  %33 = bitcast i8* %32 to %Qubit**
  %34 = load %Qubit*, %Qubit** %33, align 8
  %35 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 3)
  %36 = bitcast i8* %35 to %Qubit**
  %37 = load %Qubit*, %Qubit** %36, align 8
  %38 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 3)
  %39 = bitcast i8* %38 to %Qubit**
  %40 = load %Qubit*, %Qubit** %39, align 8
  call fastcc void @Microsoft__Quantum__Qir__Emission__Majority__body(%Qubit* %34, %Qubit* %37, %Qubit* %40)
  %41 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 3)
  %42 = bitcast i8* %41 to %Qubit**
  %c = load %Qubit*, %Qubit** %42, align 8
  call void @__quantum__qis__cnot__body(%Qubit* %c, %Qubit* %cout)
  %43 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 2)
  %44 = bitcast i8* %43 to %Qubit**
  %45 = load %Qubit*, %Qubit** %44, align 8
  %46 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 3)
  %47 = bitcast i8* %46 to %Qubit**
  %48 = load %Qubit*, %Qubit** %47, align 8
  %49 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 3)
  %50 = bitcast i8* %49 to %Qubit**
  %51 = load %Qubit*, %Qubit** %50, align 8
  call fastcc void @Microsoft__Quantum__Qir__Emission__Majority__adj(%Qubit* %45, %Qubit* %48, %Qubit* %51)
  %52 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 1)
  %53 = bitcast i8* %52 to %Qubit**
  %54 = load %Qubit*, %Qubit** %53, align 8
  %55 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 2)
  %56 = bitcast i8* %55 to %Qubit**
  %57 = load %Qubit*, %Qubit** %56, align 8
  %58 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 2)
  %59 = bitcast i8* %58 to %Qubit**
  %60 = load %Qubit*, %Qubit** %59, align 8
  call fastcc void @Microsoft__Quantum__Qir__Emission__Majority__adj(%Qubit* %54, %Qubit* %57, %Qubit* %60)
  %61 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 0)
  %62 = bitcast i8* %61 to %Qubit**
  %63 = load %Qubit*, %Qubit** %62, align 8
  %64 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 1)
  %65 = bitcast i8* %64 to %Qubit**
  %66 = load %Qubit*, %Qubit** %65, align 8
  %67 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 1)
  %68 = bitcast i8* %67 to %Qubit**
  %69 = load %Qubit*, %Qubit** %68, align 8
  call fastcc void @Microsoft__Quantum__Qir__Emission__Majority__adj(%Qubit* %63, %Qubit* %66, %Qubit* %69)
  %70 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 0)
  %71 = bitcast i8* %70 to %Qubit**
  %72 = load %Qubit*, %Qubit** %71, align 8
  %73 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 0)
  %74 = bitcast i8* %73 to %Qubit**
  %75 = load %Qubit*, %Qubit** %74, align 8
  call fastcc void @Microsoft__Quantum__Qir__Emission__Majority__adj(%Qubit* %cin, %Qubit* %72, %Qubit* %75)
  %76 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* nonnull @Microsoft__Quantum__Qir__Emission__M, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %77 = call fastcc %Array* @Microsoft__Quantum__Qir__Emission___73da7dcac81a47ddabb1a0e30be3dfdb_ForEach__body(%Callable* %76, %Array* %b)
  call void @__quantum__rt__array_update_alias_count(%Array* %b, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %76, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %76, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %cin)
  call void @__quantum__rt__qubit_release(%Qubit* %cout)
  call void @__quantum__rt__qubit_release_array(%Array* %b)
  call void @__quantum__rt__qubit_release_array(%Array* %a)
  ret %Array* %77
}

declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

declare %Array* @__quantum__rt__qubit_allocate_array(i64) local_unnamed_addr

declare void @__quantum__rt__qubit_release_array(%Array*) local_unnamed_addr

declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

declare i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64) local_unnamed_addr

declare void @__quantum__qis__x__body(%Qubit*) local_unnamed_addr

declare i64 @__quantum__rt__array_get_size_1d(%Array*) local_unnamed_addr

define internal fastcc %Array* @Microsoft__Quantum__Qir__Emission___73da7dcac81a47ddabb1a0e30be3dfdb_ForEach__body(%Callable* %action, %Array* %array) unnamed_addr {
entry:
  call void @__quantum__rt__capture_update_alias_count(%Callable* %action, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %action, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i32 1)
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  %1 = call i64 @__quantum__rt__array_get_size_1d(%Array* %array)
  %2 = add i64 %1, -1
  %.not9 = icmp slt i64 %2, 0
  br i1 %.not9, label %exit__1, label %body__1

body__1:                                          ; preds = %entry, %exit__4
  %3 = phi i64 [ %32, %exit__4 ], [ 0, %entry ]
  %res.010 = phi %Array* [ %14, %exit__4 ], [ %0, %entry ]
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array, i64 %3)
  %5 = bitcast i8* %4 to %Qubit**
  %item = load %Qubit*, %Qubit** %5, align 8
  %6 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %7 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %6, i64 0)
  %8 = bitcast i8* %7 to %Result**
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 8)
  %10 = bitcast %Tuple* %9 to %Qubit**
  store %Qubit* %item, %Qubit** %10, align 8
  %11 = call %Tuple* @__quantum__rt__tuple_create(i64 8)
  call void @__quantum__rt__callable_invoke(%Callable* %action, %Tuple* %9, %Tuple* %11)
  %12 = bitcast %Tuple* %11 to %Result**
  %13 = load %Result*, %Result** %12, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i32 -1)
  store %Result* %13, %Result** %8, align 8
  %14 = call %Array* @__quantum__rt__array_concatenate(%Array* %res.010, %Array* %6)
  %15 = call i64 @__quantum__rt__array_get_size_1d(%Array* %14)
  %16 = add i64 %15, -1
  %.not57 = icmp slt i64 %16, 0
  br i1 %.not57, label %exit__2, label %body__2

exit__1:                                          ; preds = %exit__4, %entry
  %res.0.lcssa = phi %Array* [ %0, %entry ], [ %14, %exit__4 ]
  call void @__quantum__rt__capture_update_alias_count(%Callable* %action, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %action, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %res.0.lcssa, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  ret %Array* %res.0.lcssa

body__2:                                          ; preds = %body__1, %body__2
  %17 = phi i64 [ %21, %body__2 ], [ 0, %body__1 ]
  %18 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %14, i64 %17)
  %19 = bitcast i8* %18 to %Result**
  %20 = load %Result*, %Result** %19, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %20, i32 1)
  %21 = add i64 %17, 1
  %.not5 = icmp sgt i64 %21, %16
  br i1 %.not5, label %exit__2, label %body__2

exit__2:                                          ; preds = %body__2, %body__1
  call void @__quantum__rt__array_update_reference_count(%Array* %14, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %14, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %res.010, i32 -1)
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %6, i64 0)
  %23 = bitcast i8* %22 to %Result**
  %24 = load %Result*, %Result** %23, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %24, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %6, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %14, i32 -1)
  %25 = call i64 @__quantum__rt__array_get_size_1d(%Array* %res.010)
  %26 = add i64 %25, -1
  %.not68 = icmp slt i64 %26, 0
  br i1 %.not68, label %exit__4, label %body__4

body__4:                                          ; preds = %exit__2, %body__4
  %27 = phi i64 [ %31, %body__4 ], [ 0, %exit__2 ]
  %28 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %res.010, i64 %27)
  %29 = bitcast i8* %28 to %Result**
  %30 = load %Result*, %Result** %29, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %30, i32 -1)
  %31 = add i64 %27, 1
  %.not6 = icmp sgt i64 %31, %26
  br i1 %.not6, label %exit__4, label %body__4

exit__4:                                          ; preds = %body__4, %exit__2
  call void @__quantum__rt__array_update_reference_count(%Array* %res.010, i32 -1)
  %32 = add i64 %3, 1
  %.not = icmp sgt i64 %32, %2
  br i1 %.not, label %exit__1, label %body__1
}

define internal void @Microsoft__Quantum__Qir__Emission__M__body__wrapper(%Tuple* nocapture readnone %capture-tuple, %Tuple* nocapture readonly %arg-tuple, %Tuple* nocapture %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to %Qubit**
  %1 = load %Qubit*, %Qubit** %0, align 8
  %2 = call fastcc %Result* @Microsoft__Quantum__Qir__Emission__M__body(%Qubit* %1)
  %3 = bitcast %Tuple* %result-tuple to %Result**
  store %Result* %2, %Result** %3, align 8
  ret void
}

declare %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]*, [2 x void (%Tuple*, i32)*]*, %Tuple*) local_unnamed_addr

declare void @__quantum__rt__capture_update_reference_count(%Callable*, i32) local_unnamed_addr

declare void @__quantum__rt__callable_update_reference_count(%Callable*, i32) local_unnamed_addr

declare void @__quantum__rt__capture_update_alias_count(%Callable*, i32) local_unnamed_addr

declare void @__quantum__rt__callable_update_alias_count(%Callable*, i32) local_unnamed_addr

declare %Array* @__quantum__rt__array_create_1d(i32, i64) local_unnamed_addr

declare void @__quantum__rt__array_update_reference_count(%Array*, i32) local_unnamed_addr

declare void @__quantum__rt__callable_invoke(%Callable*, %Tuple*, %Tuple*) local_unnamed_addr

declare %Array* @__quantum__rt__array_concatenate(%Array*, %Array*) local_unnamed_addr

declare void @__quantum__rt__result_update_reference_count(%Result*, i32) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

define { i64, i8* }* @Microsoft__Quantum__Qir__Emission__RunAdder__Interop() local_unnamed_addr #0 {
entry:
  %0 = call fastcc %Array* @Microsoft__Quantum__Qir__Emission__RunAdder__body()
  %1 = call i64 @__quantum__rt__array_get_size_1d(%Array* %0)
  %2 = call i8* @__quantum__rt__memory_allocate(i64 %1)
  %3 = ptrtoint i8* %2 to i64
  %4 = add i64 %1, -1
  %.not5 = icmp slt i64 %4, 0
  br i1 %.not5, label %exit__1, label %body__1

body__1:                                          ; preds = %entry, %body__1
  %5 = phi i64 [ %14, %body__1 ], [ 0, %entry ]
  %6 = add i64 %5, %3
  %7 = inttoptr i64 %6 to i8*
  %8 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %5)
  %9 = bitcast i8* %8 to %Result**
  %10 = load %Result*, %Result** %9, align 8
  %11 = call %Result* @__quantum__rt__result_get_zero()
  %12 = call i1 @__quantum__rt__result_equal(%Result* %10, %Result* %11)
  %not. = xor i1 %12, true
  %13 = sext i1 %not. to i8
  store i8 %13, i8* %7, align 1
  %14 = add i64 %5, 1
  %.not = icmp sgt i64 %14, %4
  br i1 %.not, label %exit__1, label %body__1

exit__1:                                          ; preds = %body__1, %entry
  %15 = call i8* @__quantum__rt__memory_allocate(i64 16)
  %16 = bitcast i8* %15 to i64*
  store i64 %1, i64* %16, align 4
  %17 = getelementptr i8, i8* %15, i64 8
  %18 = bitcast i8* %17 to i8**
  store i8* %2, i8** %18, align 8
  %.not34 = icmp slt i64 %4, 0
  br i1 %.not34, label %exit__2, label %body__2

body__2:                                          ; preds = %exit__1, %body__2
  %19 = phi i64 [ %23, %body__2 ], [ 0, %exit__1 ]
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %19)
  %21 = bitcast i8* %20 to %Result**
  %22 = load %Result*, %Result** %21, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %22, i32 -1)
  %23 = add i64 %19, 1
  %.not3 = icmp sgt i64 %23, %4
  br i1 %.not3, label %exit__2, label %body__2

exit__2:                                          ; preds = %body__2, %exit__1
  %24 = bitcast i8* %15 to { i64, i8* }*
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  ret { i64, i8* }* %24
}

declare i8* @__quantum__rt__memory_allocate(i64) local_unnamed_addr

declare %Result* @__quantum__rt__result_get_zero() local_unnamed_addr

declare i1 @__quantum__rt__result_equal(%Result*, %Result*) local_unnamed_addr

define void @Microsoft__Quantum__Qir__Emission__RunAdder() local_unnamed_addr #1 {
entry:
  %0 = call fastcc %Array* @Microsoft__Quantum__Qir__Emission__RunAdder__body()
  %1 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @0, i64 0, i64 0))
  %2 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @1, i64 0, i64 0))
  call void @__quantum__rt__string_update_reference_count(%String* %2, i32 1)
  %3 = call i64 @__quantum__rt__array_get_size_1d(%Array* %0)
  %4 = add i64 %3, -1
  %.not7 = icmp slt i64 %4, 0
  br i1 %.not7, label %exit__1, label %body__1

body__1:                                          ; preds = %entry, %condContinue__1
  %5 = phi i64 [ %14, %condContinue__1 ], [ 0, %entry ]
  %6 = phi %String* [ %13, %condContinue__1 ], [ %2, %entry ]
  %7 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %5)
  %8 = bitcast i8* %7 to %Result**
  %9 = load %Result*, %Result** %8, align 8
  %.not5 = icmp eq %String* %6, %2
  br i1 %.not5, label %condContinue__1, label %condTrue__1

condTrue__1:                                      ; preds = %body__1
  %10 = call %String* @__quantum__rt__string_concatenate(%String* %6, %String* %1)
  call void @__quantum__rt__string_update_reference_count(%String* %6, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %body__1
  %11 = phi %String* [ %10, %condTrue__1 ], [ %6, %body__1 ]
  %12 = call %String* @__quantum__rt__result_to_string(%Result* %9)
  %13 = call %String* @__quantum__rt__string_concatenate(%String* %11, %String* %12)
  call void @__quantum__rt__string_update_reference_count(%String* %11, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %12, i32 -1)
  %14 = add i64 %5, 1
  %.not = icmp sgt i64 %14, %4
  br i1 %.not, label %exit__1, label %body__1

exit__1:                                          ; preds = %condContinue__1, %entry
  %.lcssa = phi %String* [ %2, %entry ], [ %13, %condContinue__1 ]
  %15 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i64 0, i64 0))
  %16 = call %String* @__quantum__rt__string_concatenate(%String* %.lcssa, %String* %15)
  call void @__quantum__rt__string_update_reference_count(%String* %.lcssa, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %15, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %2, i32 -1)
  call void @__quantum__rt__message(%String* %16)
  %.not46 = icmp slt i64 %4, 0
  br i1 %.not46, label %exit__2, label %body__2

body__2:                                          ; preds = %exit__1, %body__2
  %17 = phi i64 [ %21, %body__2 ], [ 0, %exit__1 ]
  %18 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %17)
  %19 = bitcast i8* %18 to %Result**
  %20 = load %Result*, %Result** %19, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %20, i32 -1)
  %21 = add i64 %17, 1
  %.not4 = icmp sgt i64 %21, %4
  br i1 %.not4, label %exit__2, label %body__2

exit__2:                                          ; preds = %body__2, %exit__1
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare %String* @__quantum__rt__string_create(i8*) local_unnamed_addr

declare %String* @__quantum__rt__string_concatenate(%String*, %String*) local_unnamed_addr

declare %String* @__quantum__rt__result_to_string(%Result*) local_unnamed_addr

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
