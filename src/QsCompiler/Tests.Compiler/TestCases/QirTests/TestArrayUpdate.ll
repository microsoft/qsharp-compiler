define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate__body(%Array* %y, i64 %a, i64 %b) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %y)
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  %0 = load %Array*, %Array** %x
  call void @__quantum__rt__array_add_access(%Array* %0)
  %1 = call %Array* @__quantum__rt__array_copy(%Array* %0, i1 false)
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 %a)
  %3 = bitcast i8* %2 to i64*
  %4 = load i64, i64* %3
  store i64 %b, i64* %3
  call void @__quantum__rt__array_remove_access(%Array* %0)
  store %Array* %1, %Array** %x
  call void @__quantum__rt__array_add_access(%Array* %1)
  %5 = load %Array*, %Array** %x
  call void @__quantum__rt__array_reference(%Array* %5)
  call void @__quantum__rt__array_remove_access(%Array* %y)
  call void @__quantum__rt__array_remove_access(%Array* %5)
  call void @__quantum__rt__array_unreference(%Array* %1)
  ret %Array* %5
}
