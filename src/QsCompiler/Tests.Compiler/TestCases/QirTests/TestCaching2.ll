define internal %Array* @Microsoft__Quantum__Testing__QIR__LengthCaching__body(%Array* %vals, i64 %step) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %vals, i32 1)
  %0 = icmp slt i64 %step, 0
  br i1 %0, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  %1 = call i64 @__quantum__rt__array_get_size_1d(%Array* %vals)
  %2 = sub i64 %1, 1
  br label %condContinue__1

condFalse__1:                                     ; preds = %entry
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %3 = phi i64 [ %2, %condTrue__1 ], [ 0, %condFalse__1 ]
  %4 = icmp slt i64 %step, 0
  br i1 %4, label %condTrue__2, label %condFalse__2

condTrue__2:                                      ; preds = %condContinue__1
  br label %condContinue__2

condFalse__2:                                     ; preds = %condContinue__1
  %5 = call i64 @__quantum__rt__array_get_size_1d(%Array* %vals)
  %6 = sub i64 %5, 1
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condTrue__2
  %7 = phi i64 [ 0, %condTrue__2 ], [ %6, %condFalse__2 ]
  %8 = insertvalue %Range zeroinitializer, i64 %3, 0
  %9 = insertvalue %Range %8, i64 %step, 1
  %10 = insertvalue %Range %9, i64 %7, 2
  %11 = call %Array* @__quantum__rt__array_slice_1d(%Array* %vals, %Range %10, i1 true)
  call void @__quantum__rt__array_update_reference_count(%Array* %11, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %vals, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %11, i32 -1)
  ret %Array* %11
}
