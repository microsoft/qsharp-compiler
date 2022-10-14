
%ArrayStorage1 = type { [0 x i2] }
%Result = type opaque
%Range = type { i64, i64, i64 }

define void @Microsoft__Quantum__Testing__QIR__BasicParameterTest({ i64, %ArrayStorage1 } %arr, %Result* %res, %Range %range, i64 %cnt, i1 %b, double %d) #0 {
entry:
  %0 = alloca %ArrayStorage1, align 8
  %1 = extractvalue { i64, %ArrayStorage1 } %arr, 0
  %2 = extractvalue { i64, %ArrayStorage1 } %arr, 1
  call void @__quantum__rt__result_update_reference_count(%Result* %res, i32 1)
  %3 = insertvalue { i64, i1, double } zeroinitializer, i64 %cnt, 0
  %4 = insertvalue { i64, i1, double } %3, i1 %b, 1
  %5 = insertvalue { i64, i1, double } %4, double %d, 2
  %6 = insertvalue { { i64, %ArrayStorage1 }, %Result*, %Range, { i64, i1, double } } zeroinitializer, { i64, %ArrayStorage1 } %arr, 0
  %7 = insertvalue { { i64, %ArrayStorage1 }, %Result*, %Range, { i64, i1, double } } %6, %Result* %res, 1
  %8 = insertvalue { { i64, %ArrayStorage1 }, %Result*, %Range, { i64, i1, double } } %7, %Range %range, 2
  %9 = insertvalue { { i64, %ArrayStorage1 }, %Result*, %Range, { i64, i1, double } } %8, { i64, i1, double } %5, 3
  call void @__quantum__rt__tuple_start_record_output()
  call void @__quantum__rt__array_start_record_output()
  %10 = extractvalue { i64, %ArrayStorage1 } %arr, 0
  %11 = sub i64 %10, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %12 = phi i64 [ 0, %entry ], [ %22, %exiting__1 ]
  %13 = icmp sle i64 %12, %11
  br i1 %13, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %14 = extractvalue { i64, %ArrayStorage1 } %arr, 1
  store %ArrayStorage1 %14, %ArrayStorage1* %0, align 1
  %15 = getelementptr %ArrayStorage1, %ArrayStorage1* %0, i32 0, i32 0, i64 %12
  %__rtrnVal12__ = load i2, i2* %15, align 1
  %16 = icmp eq i2 %__rtrnVal12__, 1
  br i1 %16, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %body__1
  br label %condContinue__1

condFalse__1:                                     ; preds = %body__1
  %17 = icmp eq i2 %__rtrnVal12__, -1
  br i1 %17, label %condTrue__2, label %condFalse__2

condTrue__2:                                      ; preds = %condFalse__1
  br label %condContinue__2

condFalse__2:                                     ; preds = %condFalse__1
  %18 = icmp eq i2 %__rtrnVal12__, -2
  %19 = select i1 %18, i64 2, i64 0
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condTrue__2
  %20 = phi i64 [ 3, %condTrue__2 ], [ %19, %condFalse__2 ]
  br label %condContinue__1

condContinue__1:                                  ; preds = %condContinue__2, %condTrue__1
  %21 = phi i64 [ 1, %condTrue__1 ], [ %20, %condContinue__2 ]
  call void @__quantum__rt__int_record_output(i64 %21)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__1
  %22 = add i64 %12, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_end_record_output()
  call void @__quantum__rt__result_record_output(%Result* %res)
  call void @__quantum__rt__array_start_record_output()
  %23 = extractvalue %Range %range, 0
  %24 = extractvalue %Range %range, 1
  %25 = extractvalue %Range %range, 2
  br label %preheader__1

preheader__1:                                     ; preds = %exit__1
  %26 = icmp sgt i64 %24, 0
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__1
  %__rtrnVal12__1 = phi i64 [ %23, %preheader__1 ], [ %30, %exiting__2 ]
  %27 = icmp sle i64 %__rtrnVal12__1, %25
  %28 = icmp sge i64 %__rtrnVal12__1, %25
  %29 = select i1 %26, i1 %27, i1 %28
  br i1 %29, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  call void @__quantum__rt__int_record_output(i64 %__rtrnVal12__1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %30 = add i64 %__rtrnVal12__1, %24
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
