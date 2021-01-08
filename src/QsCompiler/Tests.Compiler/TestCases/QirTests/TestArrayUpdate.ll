define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate__body(%Array* %y, i64 %a, i64 %b) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %y)
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  call void @__quantum__rt__array_add_access(%Array* %y)
  %0 = call %Array* @__quantum__rt__array_copy(%Array* %y, i1 false)
  %1 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %a)
  %2 = bitcast i8* %1 to i64*
  %3 = load i64, i64* %2
  store i64 %b, i64* %2
  call void @__quantum__rt__array_remove_access(%Array* %y)
  store %Array* %0, %Array** %x
  call void @__quantum__rt__array_add_access(%Array* %0)
  %4 = load %Array*, %Array** %x
  call void @__quantum__rt__array_reference(%Array* %4)
  call void @__quantum__rt__array_remove_access(%Array* %y)
  call void @__quantum__rt__array_remove_access(%Array* %4)
  call void @__quantum__rt__array_unreference(%Array* %0)
  ret %Array* %4
}
