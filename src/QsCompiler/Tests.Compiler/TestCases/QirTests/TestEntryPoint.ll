define double @Microsoft__Quantum__Testing__QIR__TestEntryPoint(i64 %a__count, double* %a, i1 %b) #0 {
entry:
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 %a__count)
  %1 = icmp sgt i64 %a__count, 0
  br i1 %1, label %copy, label %next

copy:                                             ; preds = %entry
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 0)
  %3 = mul i64 %a__count, 8
  %4 = bitcast double* %a to i8*
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %2, i8* %4, i64 %3, i1 false)
  br label %next

next:                                             ; preds = %copy, %entry
  %5 = call double @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %0, i1 %b)
  call void @__quantum__rt__array_unreference(%Array* %0)
  ret double %5
}
