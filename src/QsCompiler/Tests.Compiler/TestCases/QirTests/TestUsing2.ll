define void @Microsoft__Quantum__Testing__QIR__ArbitraryAllocation__body(i64 %max, %Qubit* %q) {
entry:
  %a = call %Qubit* @__quantum__rt__qubit_allocate()
  %b = call %Array* @__quantum__rt__qubit_allocate_array(i64 %max)
  call void @__quantum__rt__array_add_access(%Array* %b)
  %c = call %Qubit* @__quantum__rt__qubit_allocate()
  %d = call %Array* @__quantum__rt__qubit_allocate_array(i64 2)
  call void @__quantum__rt__array_add_access(%Array* %d)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %b, i64 1)
  %1 = bitcast i8* %0 to %Qubit**
  %x = load %Qubit*, %Qubit** %1
  %z = call %Qubit* @__quantum__rt__qubit_allocate()
  %2 = load %Range, %Range* @EmptyRange
  %3 = insertvalue %Range %2, i64 0, 0
  %4 = insertvalue %Range %3, i64 2, 1
  %5 = insertvalue %Range %4, i64 %max, 2
  %y = call %Array* @__quantum__rt__array_slice_1d(%Array* %b, %Range %5, i1 false)
  call void @__quantum__rt__array_add_access(%Array* %y)
  %6 = call i64 @__quantum__rt__array_get_size_1d(%Array* %y)
  %7 = icmp eq i64 %6, %max
  br i1 %7, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  call void @__quantum__rt__qubit_release(%Qubit* %z)
  call void @__quantum__rt__array_remove_access(%Array* %y)
  call void @__quantum__rt__array_unreference(%Array* %y)
  call void @__quantum__rt__qubit_release(%Qubit* %a)
  call void @__quantum__rt__qubit_release_array(%Array* %b)
  call void @__quantum__rt__qubit_release(%Qubit* %c)
  call void @__quantum__rt__qubit_release_array(%Array* %d)
  call void @__quantum__rt__array_remove_access(%Array* %b)
  call void @__quantum__rt__array_remove_access(%Array* %d)
  call void @__quantum__rt__array_unreference(%Array* %b)
  call void @__quantum__rt__array_unreference(%Array* %d)
  ret void

continue__1:                                      ; preds = %entry
  call void @__quantum__rt__qubit_release(%Qubit* %z)
  call void @__quantum__rt__array_remove_access(%Array* %y)
  call void @__quantum__rt__array_unreference(%Array* %y)
  call void @__quantum__rt__qubit_release(%Qubit* %a)
  call void @__quantum__rt__qubit_release_array(%Array* %b)
  call void @__quantum__rt__qubit_release(%Qubit* %c)
  call void @__quantum__rt__qubit_release_array(%Array* %d)
  call void @__quantum__rt__array_remove_access(%Array* %b)
  call void @__quantum__rt__array_remove_access(%Array* %d)
  call void @__quantum__rt__array_unreference(%Array* %b)
  call void @__quantum__rt__array_unreference(%Array* %d)
  ret void
}