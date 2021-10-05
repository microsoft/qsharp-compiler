define void @Microsoft__Quantum__Testing__QIR__TestEntryPoint({ i64, i64* }* %arr, i8* %str, i8 %res, { i64, i64, i64 }* %range, i64 %cnt, i8 %b, { i64, i8* }* %paulis) #2 {
entry:
  %0 = getelementptr { i64, i64* }, { i64, i64* }* %arr, i64 0, i32 0
  %1 = getelementptr { i64, i64* }, { i64, i64* }* %arr, i64 0, i32 1
  %2 = load i64, i64* %0, align 4
  %3 = load i8*, i64** %1, align 8
  %4 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 %2)
  %5 = ptrtoint i8* %3 to i64
  %6 = sub i64 %2, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %7 = phi i64 [ 0, %entry ], [ %15, %exiting__1 ]
  %8 = icmp sle i64 %7, %6
  br i1 %8, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %9 = mul i64 %7, 8
  %10 = add i64 %5, %9
  %11 = inttoptr i64 %10 to i64*
  %12 = load i64, i64* %11, align 4
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %4, i64 %7)
  %14 = bitcast i8* %13 to i64*
  store i64 %12, i64* %14, align 4
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %15 = add i64 %7, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %16 = call %String* @__quantum__rt__string_create(i8* %str)
  %17 = icmp eq i8 %res, 0
  %18 = call %Result* @__quantum__rt__result_get_zero()
  %19 = call %Result* @__quantum__rt__result_get_one()
  %20 = select i1 %17, %Result* %18, %Result* %19
  %21 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %range, i64 0, i32 0
  %22 = load i64, i64* %21, align 4
  %23 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %range, i64 0, i32 1
  %24 = load i64, i64* %23, align 4
  %25 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %range, i64 0, i32 2
  %26 = load i64, i64* %25, align 4
  %27 = load %Range, %Range* @EmptyRange, align 4
  %28 = insertvalue %Range %27, i64 %22, 0
  %29 = insertvalue %Range %28, i64 %24, 1
  %30 = insertvalue %Range %29, i64 %26, 2
  %31 = trunc i8 %b to i1
  %32 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i1 }* getelementptr ({ i64, i1 }, { i64, i1 }* null, i32 1) to i64))
  %33 = bitcast %Tuple* %32 to { i64, i1 }*
  %34 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %33, i32 0, i32 0
  %35 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %33, i32 0, i32 1
  store i64 %cnt, i64* %34, align 4
  store i1 %31, i1* %35, align 1
  %36 = getelementptr { i64, i8* }, { i64, i8* }* %paulis, i64 0, i32 0
  %37 = getelementptr { i64, i8* }, { i64, i8* }* %paulis, i64 0, i32 1
  %38 = load i64, i64* %36, align 4
  %39 = load i8*, i8** %37, align 8
  %40 = call %Array* @__quantum__rt__array_create_1d(i32 1, i64 %38)
  %41 = ptrtoint i8* %39 to i64
  %42 = sub i64 %38, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %43 = phi i64 [ 0, %exit__1 ], [ %52, %exiting__2 ]
  %44 = icmp sle i64 %43, %42
  br i1 %44, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %45 = mul i64 %43, 1
  %46 = add i64 %41, %45
  %47 = inttoptr i64 %46 to i8*
  %48 = load i8, i8* %47, align 1
  %49 = trunc i8 %48 to i2
  %50 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %40, i64 %43)
  %51 = bitcast i8* %50 to i2*
  store i2 %49, i2* %51, align 1
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %52 = add i64 %43, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %53 = call { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %4, %String* %16, %Result* %20, %Range %30, { i64, i1 }* %33, %Array* %40)
  %54 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %55 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 0
  %56 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 1
  %57 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 2
  %58 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 3
  %59 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 4
  %60 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 5
  %61 = load %Array*, %Array** %55, align 8
  %62 = load %String*, %String** %56, align 8
  %63 = load %Result*, %Result** %57, align 8
  %64 = load %Range, %Range* %58, align 4
  %65 = load { i64, i1 }*, { i64, i1 }** %59, align 8
  %66 = load %Array*, %Array** %60, align 8
  %67 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %68 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %68, i32 1)
  %69 = call i64 @__quantum__rt__array_get_size_1d(%Array* %61)
  %70 = sub i64 %69, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %71 = phi %String* [ %68, %exit__2 ], [ %81, %exiting__3 ]
  %72 = phi i64 [ 0, %exit__2 ], [ %82, %exiting__3 ]
  %73 = icmp sle i64 %72, %70
  br i1 %73, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %74 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %61, i64 %72)
  %75 = bitcast i8* %74 to i64*
  %76 = load i64, i64* %75, align 4
  %77 = icmp ne %String* %71, %68
  br i1 %77, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %body__3
  %78 = call %String* @__quantum__rt__string_concatenate(%String* %71, %String* %67)
  call void @__quantum__rt__string_update_reference_count(%String* %71, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %body__3
  %79 = phi %String* [ %78, %condTrue__1 ], [ %71, %body__3 ]
  %80 = call %String* @__quantum__rt__int_to_string(i64 %76)
  %81 = call %String* @__quantum__rt__string_concatenate(%String* %79, %String* %80)
  call void @__quantum__rt__string_update_reference_count(%String* %79, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %80, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %condContinue__1
  %82 = add i64 %72, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  %83 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @3, i32 0, i32 0))
  %84 = call %String* @__quantum__rt__string_concatenate(%String* %71, %String* %83)
  call void @__quantum__rt__string_update_reference_count(%String* %71, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %83, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %67, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %68, i32 -1)
  %85 = call %String* @__quantum__rt__string_concatenate(%String* %54, %String* %84)
  call void @__quantum__rt__string_update_reference_count(%String* %54, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %84, i32 -1)
  %86 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %87 = call %String* @__quantum__rt__string_concatenate(%String* %85, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %85, i32 -1)
  %88 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @4, i32 0, i32 0))
  %89 = call %String* @__quantum__rt__string_concatenate(%String* %88, %String* %62)
  %90 = call %String* @__quantum__rt__string_concatenate(%String* %89, %String* %88)
  call void @__quantum__rt__string_update_reference_count(%String* %89, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %88, i32 -1)
  %91 = call %String* @__quantum__rt__string_concatenate(%String* %87, %String* %90)
  call void @__quantum__rt__string_update_reference_count(%String* %87, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %90, i32 -1)
  %92 = call %String* @__quantum__rt__string_concatenate(%String* %91, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %91, i32 -1)
  %93 = call %String* @__quantum__rt__result_to_string(%Result* %63)
  %94 = call %String* @__quantum__rt__string_concatenate(%String* %92, %String* %93)
  call void @__quantum__rt__string_update_reference_count(%String* %92, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %93, i32 -1)
  %95 = call %String* @__quantum__rt__string_concatenate(%String* %94, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %94, i32 -1)
  %96 = call %String* @__quantum__rt__range_to_string(%Range %64)
  %97 = call %String* @__quantum__rt__string_concatenate(%String* %95, %String* %96)
  call void @__quantum__rt__string_update_reference_count(%String* %95, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %96, i32 -1)
  %98 = call %String* @__quantum__rt__string_concatenate(%String* %97, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %97, i32 -1)
  %99 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %100 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %65, i32 0, i32 0
  %101 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %65, i32 0, i32 1
  %102 = load i64, i64* %100, align 4
  %103 = load i1, i1* %101, align 1
  %104 = call %String* @__quantum__rt__int_to_string(i64 %102)
  %105 = call %String* @__quantum__rt__string_concatenate(%String* %99, %String* %104)
  call void @__quantum__rt__string_update_reference_count(%String* %99, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %104, i32 -1)
  %106 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %107 = call %String* @__quantum__rt__string_concatenate(%String* %105, %String* %106)
  call void @__quantum__rt__string_update_reference_count(%String* %105, i32 -1)
  br i1 %103, label %condTrue__2, label %condFalse__1

condTrue__2:                                      ; preds = %exit__3
  %108 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i32 0, i32 0))
  br label %condContinue__2

condFalse__1:                                     ; preds = %exit__3
  %109 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @6, i32 0, i32 0))
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__1, %condTrue__2
  %110 = phi %String* [ %108, %condTrue__2 ], [ %109, %condFalse__1 ]
  %111 = call %String* @__quantum__rt__string_concatenate(%String* %107, %String* %110)
  call void @__quantum__rt__string_update_reference_count(%String* %107, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %110, i32 -1)
  %112 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @7, i32 0, i32 0))
  %113 = call %String* @__quantum__rt__string_concatenate(%String* %111, %String* %112)
  call void @__quantum__rt__string_update_reference_count(%String* %111, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %112, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %106, i32 -1)
  %114 = call %String* @__quantum__rt__string_concatenate(%String* %98, %String* %113)
  call void @__quantum__rt__string_update_reference_count(%String* %98, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %113, i32 -1)
  %115 = call %String* @__quantum__rt__string_concatenate(%String* %114, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %114, i32 -1)
  %116 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %117 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %117, i32 1)
  %118 = call i64 @__quantum__rt__array_get_size_1d(%Array* %66)
  %119 = sub i64 %118, 1
  br label %header__4

header__4:                                        ; preds = %exiting__4, %condContinue__2
  %120 = phi %String* [ %117, %condContinue__2 ], [ %142, %exiting__4 ]
  %121 = phi i64 [ 0, %condContinue__2 ], [ %143, %exiting__4 ]
  %122 = icmp sle i64 %121, %119
  br i1 %122, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %123 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %66, i64 %121)
  %124 = bitcast i8* %123 to i2*
  %125 = load i2, i2* %124, align 1
  %126 = icmp ne %String* %120, %117
  br i1 %126, label %condTrue__3, label %condContinue__3

condTrue__3:                                      ; preds = %body__4
  %127 = call %String* @__quantum__rt__string_concatenate(%String* %120, %String* %116)
  call void @__quantum__rt__string_update_reference_count(%String* %120, i32 -1)
  br label %condContinue__3

condContinue__3:                                  ; preds = %condTrue__3, %body__4
  %128 = phi %String* [ %127, %condTrue__3 ], [ %120, %body__4 ]
  %129 = load i2, i2* @PauliX, align 1
  %130 = icmp eq i2 %129, %125
  br i1 %130, label %condTrue__4, label %condFalse__2

condTrue__4:                                      ; preds = %condContinue__3
  %131 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @8, i32 0, i32 0))
  br label %condContinue__4

condFalse__2:                                     ; preds = %condContinue__3
  %132 = load i2, i2* @PauliY, align 1
  %133 = icmp eq i2 %132, %125
  br i1 %133, label %condTrue__5, label %condFalse__3

condTrue__5:                                      ; preds = %condFalse__2
  %134 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @9, i32 0, i32 0))
  br label %condContinue__5

condFalse__3:                                     ; preds = %condFalse__2
  %135 = load i2, i2* @PauliZ, align 1
  %136 = icmp eq i2 %135, %125
  br i1 %136, label %condTrue__6, label %condFalse__4

condTrue__6:                                      ; preds = %condFalse__3
  %137 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @10, i32 0, i32 0))
  br label %condContinue__6

condFalse__4:                                     ; preds = %condFalse__3
  %138 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @11, i32 0, i32 0))
  br label %condContinue__6

condContinue__6:                                  ; preds = %condFalse__4, %condTrue__6
  %139 = phi %String* [ %137, %condTrue__6 ], [ %138, %condFalse__4 ]
  br label %condContinue__5

condContinue__5:                                  ; preds = %condContinue__6, %condTrue__5
  %140 = phi %String* [ %134, %condTrue__5 ], [ %139, %condContinue__6 ]
  br label %condContinue__4

condContinue__4:                                  ; preds = %condContinue__5, %condTrue__4
  %141 = phi %String* [ %131, %condTrue__4 ], [ %140, %condContinue__5 ]
  %142 = call %String* @__quantum__rt__string_concatenate(%String* %128, %String* %141)
  call void @__quantum__rt__string_update_reference_count(%String* %128, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %141, i32 -1)
  br label %exiting__4

exiting__4:                                       ; preds = %condContinue__4
  %143 = add i64 %121, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  %144 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @3, i32 0, i32 0))
  %145 = call %String* @__quantum__rt__string_concatenate(%String* %120, %String* %144)
  call void @__quantum__rt__string_update_reference_count(%String* %120, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %144, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %116, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %117, i32 -1)
  %146 = call %String* @__quantum__rt__string_concatenate(%String* %115, %String* %145)
  call void @__quantum__rt__string_update_reference_count(%String* %115, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %145, i32 -1)
  %147 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @7, i32 0, i32 0))
  %148 = call %String* @__quantum__rt__string_concatenate(%String* %146, %String* %147)
  call void @__quantum__rt__string_update_reference_count(%String* %146, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %147, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %86, i32 -1)
  call void @__quantum__rt__message(%String* %148)
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %32, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %40, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %61, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %62, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %63, i32 -1)
  %149 = bitcast { i64, i1 }* %65 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %149, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %66, i32 -1)
  %150 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %150, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %148, i32 -1)
  ret void
}
