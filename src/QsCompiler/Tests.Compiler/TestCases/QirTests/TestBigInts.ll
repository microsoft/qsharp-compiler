define %BigInt* @Microsoft__Quantum__Testing__QIR__TestBigInts__body(%BigInt* %a, %BigInt* %b) {
entry:
  %0 = call i1 @__quantum__rt__bigint_greater(%BigInt* %a, %BigInt* %b)
  %c = select i1 %0, %BigInt* %a, %BigInt* %b
  %1 = call %BigInt* @__quantum__rt__bigint_multiply(%BigInt* %c, %BigInt* %a)
  %2 = call %BigInt* @__quantum__rt__bigint_create_i64(i64 7)
  %3 = call %BigInt* @__quantum__rt__bigint_divide(%BigInt* %b, %BigInt* %2)
  %d = call %BigInt* @__quantum__rt__bigint_subtract(%BigInt* %1, %BigInt* %3)
  %e = call %BigInt* @__quantum__rt__bigint_shiftright(%BigInt* %d, i64 3)
  %f = call %BigInt* @__quantum__rt__bigint_power(%BigInt* %d, i32 5)
  %4 = call %BigInt* @__quantum__rt__bigint_bitand(%BigInt* %e, %BigInt* %f)
  %5 = call %BigInt* @__quantum__rt__bigint_create_i64(i64 65535)
  %g = call %BigInt* @__quantum__rt__bigint_bitor(%BigInt* %4, %BigInt* %5)
  %6 = call %BigInt* @__quantum__rt__bigint_bitnot(%BigInt* %g)
  call void @__quantum__rt__bigint_unreference(%BigInt* %1)
  call void @__quantum__rt__bigint_unreference(%BigInt* %2)
  call void @__quantum__rt__bigint_unreference(%BigInt* %3)
  call void @__quantum__rt__bigint_unreference(%BigInt* %d)
  call void @__quantum__rt__bigint_unreference(%BigInt* %e)
  call void @__quantum__rt__bigint_unreference(%BigInt* %f)
  call void @__quantum__rt__bigint_unreference(%BigInt* %4)
  call void @__quantum__rt__bigint_unreference(%BigInt* %5)
  call void @__quantum__rt__bigint_unreference(%BigInt* %g)
  ret %BigInt* %6
}
