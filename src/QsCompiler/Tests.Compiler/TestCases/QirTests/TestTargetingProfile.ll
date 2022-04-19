define internal %Result* @Microsoft__Quantum__Testing__QIR__TestProfileTargeting__body() {
entry:
  %0 = alloca [3 x i64], align 8
  store [3 x i64] [i64 1, i64 2, i64 3], [3 x i64]* %0, align 4
  %1 = bitcast [3 x i64]* %0 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %1)
  %sum = alloca i64, align 8
  store i64 0, i64* %sum, align 4
  store i64 1, i64* %sum, align 4
  store i64 3, i64* %sum, align 4
  store i64 6, i64* %sum, align 4
  %2 = alloca [3 x i64], align 8
  store [3 x i64] [i64 6, i64 6, i64 6], [3 x i64]* %2, align 4
  %3 = bitcast [3 x i64]* %2 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %3)
  %4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %5 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %4, 0
  %6 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qs = insertvalue [2 x %Qubit*] %5, %Qubit* %6, 1
  %qubit = extractvalue [2 x %Qubit*] %qs, 0
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  %control = extractvalue [2 x %Qubit*] %qs, 0
  %target = extractvalue [2 x %Qubit*] %qs, 1
  call void @__quantum__qis__cnot__body(%Qubit* %control, %Qubit* %target)
  %qubit__1 = extractvalue [2 x %Qubit*] %qs, 0
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__1)
  %qubit__2 = extractvalue [2 x %Qubit*] %qs, 1
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__2)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %4)
  call void @__quantum__rt__qubit_release(%Qubit* %6)
  ret %Result* %m1
}
