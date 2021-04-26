define %Array* @Microsoft__Quantum__Testing__QIR__LengthCaching__body(%Array* %vals) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %vals, i32 1)
  br i1 false, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  %0 = call i64 @__quantum__rt__array_get_size_1d(%Array* %vals)
  %1 = sub i64 %0, 1
  br label %condContinue__1

condFalse__1:                                     ; preds = %entry
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %2 = phi i64 [ %1, %condTrue__1 ], [ 0, %condFalse__1 ]
  br i1 false, label %condTrue__2, label %condFalse__2

condTrue__2:                                      ; preds = %condContinue__1
  br label %condContinue__2

condFalse__2:                                     ; preds = %condContinue__1
  %3 = call i64 @__quantum__rt__array_get_size_1d(%Array* %vals)
  %4 = sub i64 %3, 1
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condTrue__2
  %5 = phi i64 [ 0, %condTrue__2 ], [ %4, %condFalse__2 ]
  %6 = load %Range, %Range* @EmptyRange, align 4
  %7 = insertvalue %Range %6, i64 %2, 0
  %8 = insertvalue %Range %7, i64 2, 1
  %9 = insertvalue %Range %8, i64 %5, 2
  %10 = call %Array* @__quantum__rt__array_slice_1d(%Array* %vals, %Range %9, i1 true)
  call void @__quantum__rt__array_update_reference_count(%Array* %10, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %vals, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %10, i32 -1)
  ret %Array* %10
}
