define %String* @Microsoft__Quantum__Testing__QIR__TestStrings__body(i64 %a, i64 %b) {
entry:
  %0 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %1 = call %String* @__quantum__rt__int_to_string(i64 %a)
  %2 = call %String* @__quantum__rt__string_concatenate(%String* %0, %String* %1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %1, i64 -1)
  %3 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @1, i32 0, i32 0))
  %x = call %String* @__quantum__rt__string_concatenate(%String* %2, %String* %3)
  call void @__quantum__rt__string_update_reference_count(%String* %2, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %3, i64 -1)
  %4 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([8 x i8], [8 x i8]* @2, i32 0, i32 0))
  %5 = add i64 %a, %b
  %6 = call %String* @__quantum__rt__int_to_string(i64 %5)
  %y = call %String* @__quantum__rt__string_concatenate(%String* %4, %String* %6)
  call void @__quantum__rt__string_update_reference_count(%String* %4, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %6, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %y, i64 1)
  %7 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([17 x i8], [17 x i8]* @3, i32 0, i32 0))
  %8 = call %String* @__quantum__rt__double_to_string(double 1.200000e+00)
  %9 = call %String* @__quantum__rt__string_concatenate(%String* %7, %String* %8)
  call void @__quantum__rt__string_update_reference_count(%String* %7, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 -1)
  %10 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([7 x i8], [7 x i8]* @4, i32 0, i32 0))
  %11 = call %String* @__quantum__rt__string_concatenate(%String* %9, %String* %10)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %10, i64 -1)
  %12 = call %String* @__quantum__rt__bool_to_string(i1 true)
  %13 = call %String* @__quantum__rt__string_concatenate(%String* %11, %String* %12)
  call void @__quantum__rt__string_update_reference_count(%String* %11, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %12, i64 -1)
  %14 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([8 x i8], [8 x i8]* @5, i32 0, i32 0))
  %15 = call %String* @__quantum__rt__string_concatenate(%String* %13, %String* %14)
  call void @__quantum__rt__string_update_reference_count(%String* %13, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %14, i64 -1)
  %16 = load i2, i2* @PauliX, align 1
  %17 = call %String* @__quantum__rt__pauli_to_string(i2 %16)
  %18 = call %String* @__quantum__rt__string_concatenate(%String* %15, %String* %17)
  call void @__quantum__rt__string_update_reference_count(%String* %15, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i64 -1)
  %19 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([9 x i8], [9 x i8]* @6, i32 0, i32 0))
  %20 = call %String* @__quantum__rt__string_concatenate(%String* %18, %String* %19)
  call void @__quantum__rt__string_update_reference_count(%String* %18, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %19, i64 -1)
  %21 = load %Result*, %Result** @ResultOne, align 8
  %22 = call %String* @__quantum__rt__result_to_string(%Result* %21)
  %23 = call %String* @__quantum__rt__string_concatenate(%String* %20, %String* %22)
  call void @__quantum__rt__string_update_reference_count(%String* %20, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %22, i64 -1)
  %24 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([9 x i8], [9 x i8]* @7, i32 0, i32 0))
  %25 = call %String* @__quantum__rt__string_concatenate(%String* %23, %String* %24)
  call void @__quantum__rt__string_update_reference_count(%String* %23, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %24, i64 -1)
  %26 = call %BigInt* @__quantum__rt__bigint_create_i64(i64 1)
  %27 = call %String* @__quantum__rt__bigint_to_string(%BigInt* %26)
  %28 = call %String* @__quantum__rt__string_concatenate(%String* %25, %String* %27)
  call void @__quantum__rt__string_update_reference_count(%String* %25, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %27, i64 -1)
  %29 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([8 x i8], [8 x i8]* @8, i32 0, i32 0))
  %30 = call %String* @__quantum__rt__string_concatenate(%String* %28, %String* %29)
  call void @__quantum__rt__string_update_reference_count(%String* %28, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %29, i64 -1)
  %31 = load %Range, %Range* @EmptyRange, align 4
  %32 = insertvalue %Range %31, i64 0, 0
  %33 = insertvalue %Range %32, i64 1, 1
  %34 = insertvalue %Range %33, i64 3, 2
  %35 = call %String* @__quantum__rt__range_to_string(%Range %34)
  %i = call %String* @__quantum__rt__string_concatenate(%String* %30, %String* %35)
  call void @__quantum__rt__string_update_reference_count(%String* %30, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %35, i64 -1)
  %36 = call %String* @__quantum__rt__string_create(i32 0, i8* getelementptr inbounds ([8 x i8], [8 x i8]* @9, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %x, i64 1)
  %37 = call %String* @__quantum__rt__string_concatenate(%String* %36, %String* %x)
  call void @__quantum__rt__string_update_reference_count(%String* %36, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %x, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %x, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %y, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %y, i64 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %26, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %i, i64 -1)
  ret %String* %37
}
