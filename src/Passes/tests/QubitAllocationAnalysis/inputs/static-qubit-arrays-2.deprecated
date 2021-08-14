; ModuleID = 'qir/ConstSizeArray.ll'
source_filename = "qir/ConstSizeArray.ll"

%Array = type opaque
%String = type opaque

define internal fastcc void @Example__Main__body() unnamed_addr {
entry:
  call fastcc void @Example__QuantumProgram__body(i64 3, i64 2, i64 1)
  call fastcc void @Example__QuantumProgram__body(i64 4, i64 9, i64 4)
  ret void
}

define internal fastcc void @Example__QuantumProgram__body(i64 %x, i64 %h, i64 %g) unnamed_addr {
entry:
  %.neg = xor i64 %x, -1
  %.neg1 = mul i64 %.neg, %x
  %z.neg = add i64 %.neg1, 47
  %y = mul i64 %x, 3
  %qubits0 = call %Array* @__quantum__rt__qubit_allocate_array(i64 9)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits0, i32 1)
  %0 = add i64 %y, -2
  %1 = lshr i64 %0, 1
  %2 = add i64 %z.neg, %1
  %qubits1 = call %Array* @__quantum__rt__qubit_allocate_array(i64 %2)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits1, i32 1)
  %3 = sub i64 %y, %g
  %qubits2 = call %Array* @__quantum__rt__qubit_allocate_array(i64 %3)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits2, i32 1)
  %qubits3 = call %Array* @__quantum__rt__qubit_allocate_array(i64 %h)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits3, i32 1)
  %4 = call fastcc i64 @Example__X__body(i64 %x)
  %qubits4 = call %Array* @__quantum__rt__qubit_allocate_array(i64 %4)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits4, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits4, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits4)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits3, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits3)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits2, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits2)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits1, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits1)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits0, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits0)
  ret void
}

declare %Array* @__quantum__rt__qubit_allocate_array(i64) local_unnamed_addr

declare void @__quantum__rt__qubit_release_array(%Array*) local_unnamed_addr

declare void @__quantum__rt__array_update_alias_count(%Array*, i32) local_unnamed_addr

; Function Attrs: norecurse nounwind readnone willreturn
define internal fastcc i64 @Example__X__body(i64 %value) unnamed_addr #0 {
entry:
  %0 = mul i64 %value, 3
  ret i64 %0
}

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

define i64 @Example__Main__Interop() local_unnamed_addr #1 {
entry:
  call fastcc void @Example__Main__body()
  ret i64 0
}

define void @Example__Main() local_unnamed_addr #2 {
entry:
  call fastcc void @Example__Main__body()
  %0 = call %String* @__quantum__rt__int_to_string(i64 0)
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

attributes #0 = { norecurse nounwind readnone willreturn }
attributes #1 = { "InteropFriendly" }
attributes #2 = { "EntryPoint" }
