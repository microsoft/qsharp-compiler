define internal %Array* @Microsoft__Quantum__Testing__QIR__SlicingWithOpenEndedRange__body(%Array* %arr) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 1)
  %0 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %1 = sub i64 %0, 1
  %2 = insertvalue %Range { i64 0, i64 2, i64 0 }, i64 %1, 2
  %3 = call %Array* @__quantum__rt__array_slice_1d(%Array* %arr, %Range %2, i1 true)
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i32 -1)
  ret %Array* %3
}
