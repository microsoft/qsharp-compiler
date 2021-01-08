define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate__body(%Array* %y, i64 %a, i64 %b) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %y)
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  %0 = load %Array*, %Array** %x
  call void @__quantum__rt__array_add_access(%Array* %0)
  %1 = load %Array*, %Array** %x
  %2 = call %Array* @__quantum__rt__array_copy(%Array* %1, i1 false)
  %3 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %2, i64 %a)
  %4 = bitcast i8* %3 to i64*
  %5 = load i64, i64* %4
  store i64 %b, i64* %4
  %6 = load %Array*, %Array** %x
  call void @__quantum__rt__array_remove_access(%Array* %6)
  store %Array* %2, %Array** %x
  call void @__quantum__rt__array_add_access(%Array* %2)
  %7 = load %Array*, %Array** %x
  call void @__quantum__rt__array_reference(%Array* %7)
  call void @__quantum__rt__array_remove_access(%Array* %y)
  %8 = load %Array*, %Array** %x
  call void @__quantum__rt__array_remove_access(%Array* %8)
  call void @__quantum__rt__array_unreference(%Array* %2)
  ret %Array* %7
}
