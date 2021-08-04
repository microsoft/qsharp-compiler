; ModuleID = 'qir/ConstSizeArray.ll'
source_filename = "qir/ConstSizeArray.ll"

%Array = type opaque
%Qubit = type opaque
%String = type opaque

define internal fastcc void @Example__Main__body() unnamed_addr {
entry:
  %qubits2 = call %Array* @__quantum__rt__qubit_allocate_array(i64 3)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits2, i32 1)
  %qubits1 = call %Array* @__quantum__rt__qubit_allocate_array(i64 3)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits1, i32 1)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qubits1, i64 0)
  %1 = bitcast i8* %0 to %Qubit**
  %qubit = load %Qubit*, %Qubit** %1, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit)
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qubits1, i64 1)
  %3 = bitcast i8* %2 to %Qubit**
  %qubit__1 = load %Qubit*, %Qubit** %3, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit__1)
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qubits1, i64 2)
  %5 = bitcast i8* %4 to %Qubit**
  %qubit__2 = load %Qubit*, %Qubit** %5, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit__2)
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qubits2, i64 0)
  %7 = bitcast i8* %6 to %Qubit**
  %qubit__3 = load %Qubit*, %Qubit** %7, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit__3)
  %8 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qubits2, i64 1)
  %9 = bitcast i8* %8 to %Qubit**
  %qubit__4 = load %Qubit*, %Qubit** %9, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit__4)
  %10 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qubits2, i64 2)
  %11 = bitcast i8* %10 to %Qubit**
  %qubit__5 = load %Qubit*, %Qubit** %11, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit__5)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits1, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits2, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits2)
  ret void
}

declare %Array* @__quantum__rt__qubit_allocate_array(i64) local_unnamed_addr

declare void @__quantum__rt__qubit_release_array(%Array*) local_unnamed_addr

declare void @__quantum__rt__array_update_alias_count(%Array*, i32) local_unnamed_addr

declare i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64) local_unnamed_addr

declare void @__quantum__qis__x__body(%Qubit*) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

define i64 @Example__Main__Interop() local_unnamed_addr #0 {
entry:
  call fastcc void @Example__Main__body()
  ret i64 0
}

define void @Example__Main() local_unnamed_addr #1 {
entry:
  call fastcc void @Example__Main__body()
  %0 = call %String* @__quantum__rt__int_to_string(i64 0)
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
