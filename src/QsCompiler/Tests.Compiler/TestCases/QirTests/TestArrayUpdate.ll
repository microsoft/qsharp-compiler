define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate__body(%Array* %y, i64 %a, i64 %b) {
entry:
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  %0 = load %Array*, %Array** %x
  %1 = call %Array* @__quantum__rt__array_copy(%Array* %0)
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 %a)
  %3 = bitcast i8* %2 to i64*
  store i64 %b, i64* %3
  %4 = load %Array*, %Array** %x
  store %Array* %1, %Array** %x
  call void @__quantum__rt__array_reference(%Array* %1)
  %5 = load %Array*, %Array** %x
  call void @__quantum__rt__array_unreference(%Array* %1)
  call void @__quantum__rt__array_unreference(%Array* %4)
  ret %Array* %5
}
