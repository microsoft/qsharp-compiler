define internal void @Microsoft__Quantum__Testing__QIR__Main__body() {
entry:
  %0 = call %BigInt* @__quantum__rt__bigint_create_i64(i64 0)
  %1 = call %BigInt* @__quantum__rt__bigint_create_i64(i64 0)
  %2 = call %BigInt* @Microsoft__Quantum__Testing__QIR__TestBigInts__body(%BigInt* %0, %BigInt* %1)
  %N1 = call %BigInt* @__quantum__rt__bigint_create_i64(i64 12345)
  %N2 = call %BigInt* @__quantum__rt__bigint_create_array(i32 42, i8* getelementptr inbounds ([42 x i8], [42 x i8]* @0, i32 0, i32 0))
  %3 = call %BigInt* @Microsoft__Quantum__Testing__QIR__TestBigInts__body(%BigInt* %N1, %BigInt* %N2)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %0, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %1, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %2, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %N1, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %N2, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %3, i32 -1)
  ret void
}
