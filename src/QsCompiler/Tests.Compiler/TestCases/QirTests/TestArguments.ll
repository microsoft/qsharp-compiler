
%ArrayStorage1 = type { [0 x i2] }
%Result = type opaque
%Range = type { i64, i64, i64 }

define void @Microsoft__Quantum__Testing__QIR__BasicParameterTest({ i64, %ArrayStorage1 } %arr, %Result* %res, %Range %range, { i64, i1, double }* %0) #0 {
entry:
  %1 = alloca %ArrayStorage1, align 8
  %2 = extractvalue { i64, %ArrayStorage1 } %arr, 0
  %3 = extractvalue { i64, %ArrayStorage1 } %arr, 1
  %4 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %0, i32 0, i32 0
  %cnt = load i64, i64* %4, align 4
  %5 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %0, i32 0, i32 1
  %b = load i1, i1* %5, align 1
  %6 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %0, i32 0, i32 2
  %d = load double, double* %6, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %res, i32 1)
  %7 = insertvalue { i64, i1, double } zeroinitializer, i64 %cnt, 0
  %8 = insertvalue { i64, i1, double } %7, i1 %b, 1
  %9 = insertvalue { i64, i1, double } %8, double %d, 2
  %10 = insertvalue { { i64, %ArrayStorage1 }, %Result*, %Range, { i64, i1, double } } zeroinitializer, { i64, %ArrayStorage1 } %arr, 0
  %11 = insertvalue { { i64, %ArrayStorage1 }, %Result*, %Range, { i64, i1, double } } %10, %Result* %res, 1
  %12 = insertvalue { { i64, %ArrayStorage1 }, %Result*, %Range, { i64, i1, double } } %11, %Range %range, 2
  %13 = insertvalue { { i64, %ArrayStorage1 }, %Result*, %Range, { i64, i1, double } } %12, { i64, i1, double } %9, 3
  call void @__quantum__rt__tuple_start_record_output()
  call void @__quantum__rt__array_start_record_output()
  %14 = extractvalue { i64, %ArrayStorage1 } %arr, 0
  %15 = sub i64 %14, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %16 = phi i64 [ 0, %entry ], [ %26, %exiting__1 ]
  %17 = icmp sle i64 %16, %15
  br i1 %17, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %18 = extractvalue { i64, %ArrayStorage1 } %arr, 1
  store %ArrayStorage1 %18, %ArrayStorage1* %1, align 1
  %19 = getelementptr %ArrayStorage1, %ArrayStorage1* %1, i32 0, i32 0, i64 %16
  %__rtrnVal12__ = load i2, i2* %19, align 1
  %20 = icmp eq i2 %__rtrnVal12__, 1
  br i1 %20, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %body__1
  br label %condContinue__1

condFalse__1:                                     ; preds = %body__1
  %21 = icmp eq i2 %__rtrnVal12__, -1
  br i1 %21, label %condTrue__2, label %condFalse__2

condTrue__2:                                      ; preds = %condFalse__1
  br label %condContinue__2

condFalse__2:                                     ; preds = %condFalse__1
  %22 = icmp eq i2 %__rtrnVal12__, -2
  %23 = select i1 %22, i64 2, i64 0
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condTrue__2
  %24 = phi i64 [ 3, %condTrue__2 ], [ %23, %condFalse__2 ]
  br label %condContinue__1

condContinue__1:                                  ; preds = %condContinue__2, %condTrue__1
  %25 = phi i64 [ 1, %condTrue__1 ], [ %24, %condContinue__2 ]
  call void @__quantum__rt__int_record_output(i64 %25)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__1
  %26 = add i64 %16, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_end_record_output()
  call void @__quantum__rt__result_record_output(%Result* %res)
  call void @__quantum__rt__array_start_record_output()
  %27 = extractvalue %Range %range, 0
  %28 = extractvalue %Range %range, 1
  %29 = extractvalue %Range %range, 2
  br label %preheader__1

preheader__1:                                     ; preds = %exit__1
  %30 = icmp sgt i64 %28, 0
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__1
  %__rtrnVal12__1 = phi i64 [ %27, %preheader__1 ], [ %34, %exiting__2 ]
  %31 = icmp sle i64 %__rtrnVal12__1, %29
  %32 = icmp sge i64 %__rtrnVal12__1, %29
  %33 = select i1 %30, i1 %31, i1 %32
  br i1 %33, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  call void @__quantum__rt__int_record_output(i64 %__rtrnVal12__1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %34 = add i64 %__rtrnVal12__1, %28
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_end_record_output()
  call void @__quantum__rt__tuple_start_record_output()
  call void @__quantum__rt__int_record_output(i64 %cnt)
  call void @__quantum__rt__bool_record_output(i1 %b)
  call void @__quantum__rt__double_record_output(double %d)
  call void @__quantum__rt__tuple_end_record_output()
  call void @__quantum__rt__tuple_end_record_output()
  ret void
}

declare void @__quantum__rt__result_update_reference_count(%Result*, i32)

declare void @__quantum__rt__tuple_start_record_output()

declare void @__quantum__rt__array_start_record_output()

declare void @__quantum__rt__int_record_output(i64)

declare void @__quantum__rt__array_end_record_output()

declare void @__quantum__rt__result_record_output(%Result*)

declare void @__quantum__rt__bool_record_output(i1)

declare void @__quantum__rt__double_record_output(double)

declare void @__quantum__rt__tuple_end_record_output()

attributes #0 = { "EntryPoint" }
