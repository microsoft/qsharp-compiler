; ModuleID = 'bridge'
source_filename = "bridge"

%Qubit = type opaque
%class.QUBIT = type opaque
%Result = type opaque
%class.RESULT = type opaque
%Array = type opaque
%"struct.quantum::Array" = type opaque
%TupleHeader = type { i32 }
%"struct.quantum::TupleHeader" = type opaque
%String = type opaque

define double @__quantum__qis__intAsDouble(i64 %arg1) {
entry:
  %0 = call double @intAsDouble(i64 %arg1)
  ret double %0
}

declare double @intAsDouble(i64)

define void @__quantum__qis__cnot(%Qubit* %arg1, %Qubit* %arg2) {
entry:
  %0 = bitcast %Qubit* %arg1 to %class.QUBIT*
  %1 = bitcast %Qubit* %arg2 to %class.QUBIT*
  call void @cnot(%class.QUBIT* %0, %class.QUBIT* %1)
  ret void
}

declare void @cnot(%class.QUBIT*, %class.QUBIT*)

define void @__quantum__qis__h(%Qubit* %arg1) {
entry:
  %0 = bitcast %Qubit* %arg1 to %class.QUBIT*
  call void @h(%class.QUBIT* %0)
  ret void
}

declare void @h(%class.QUBIT*)

define %Result* @__quantum__qis__mz(%Qubit* %arg1) {
entry:
  %0 = bitcast %Qubit* %arg1 to %class.QUBIT*
  %1 = call %class.RESULT* @mz(%class.QUBIT* %0)
  %2 = bitcast %class.RESULT* %1 to %Result*
  ret %Result* %2
}

declare %class.RESULT* @mz(%class.QUBIT*)

define %Result* @__quantum__qis__measure(%Array* %arg1, %Array* %arg2) {
entry:
  %0 = bitcast %Array* %arg1 to %"struct.quantum::Array"*
  %1 = bitcast %Array* %arg2 to %"struct.quantum::Array"*
  %2 = call %class.RESULT* @measure(%"struct.quantum::Array"* %0, %"struct.quantum::Array"* %1)
  %3 = bitcast %class.RESULT* %2 to %Result*
  ret %Result* %3
}

declare %class.RESULT* @measure(%"struct.quantum::Array"*, %"struct.quantum::Array"*)

define void @__quantum__qis__rx(double %arg1, %Qubit* %arg2) {
entry:
  %0 = bitcast %Qubit* %arg2 to %class.QUBIT*
  call void @rx(double %arg1, %class.QUBIT* %0)
  ret void
}

declare void @rx(double, %class.QUBIT*)

define void @__quantum__qis__rz(double %arg1, %Qubit* %arg2) {
entry:
  %0 = bitcast %Qubit* %arg2 to %class.QUBIT*
  call void @rz(double %arg1, %class.QUBIT* %0)
  ret void
}

declare void @rz(double, %class.QUBIT*)

define void @__quantum__qis__s(%Qubit* %arg1) {
entry:
  %0 = bitcast %Qubit* %arg1 to %class.QUBIT*
  call void @s(%class.QUBIT* %0)
  ret void
}

declare void @s(%class.QUBIT*)

define void @__quantum__qis__x(%Qubit* %arg1) {
entry:
  %0 = bitcast %Qubit* %arg1 to %class.QUBIT*
  call void @x(%class.QUBIT* %0)
  ret void
}

declare void @x(%class.QUBIT*)

define void @__quantum__qis__z(%Qubit* %arg1) {
entry:
  %0 = bitcast %Qubit* %arg1 to %class.QUBIT*
  call void @z(%class.QUBIT* %0)
  ret void
}

declare void @z(%class.QUBIT*)

define %Qubit* @__quantum__rt__qubit_allocate() {
entry:
  %0 = call %class.QUBIT* @qubit_allocate()
  %1 = bitcast %class.QUBIT* %0 to %Qubit*
  ret %Qubit* %1
}

declare %class.QUBIT* @qubit_allocate()

define %Array* @__quantum__rt__qubit_allocate_array(i64 %arg1) {
entry:
  %0 = call %"struct.quantum::Array"* @qubit_allocate_array(i64 %arg1)
  %1 = bitcast %"struct.quantum::Array"* %0 to %Array*
  ret %Array* %1
}

declare %"struct.quantum::Array"* @qubit_allocate_array(i64)

define %Array* @__quantum__rt__array_create_1d(i32 %arg1, i64 %arg2) {
entry:
  %0 = call %"struct.quantum::Array"* @array_create_1d(i32 %arg1, i64 %arg2)
  %1 = bitcast %"struct.quantum::Array"* %0 to %Array*
  ret %Array* %1
}

declare %"struct.quantum::Array"* @array_create_1d(i32, i64)

define i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arg1, i64 %arg2) {
entry:
  %0 = bitcast %Array* %arg1 to %"struct.quantum::Array"*
  %1 = call i8* @array_get_element_ptr_1d(%"struct.quantum::Array"* %0, i64 %arg2)
  ret i8* %1
}

declare i8* @array_get_element_ptr_1d(%"struct.quantum::Array"*, i64)

define void @__quantum__rt__qubit_release(%Qubit* %arg1) {
entry:
  %0 = bitcast %Qubit* %arg1 to %class.QUBIT*
  call void @qubit_release(%class.QUBIT* %0)
  ret void
}

declare void @qubit_release(%class.QUBIT*)

define void @__quantum__rt__array_unreference(%Array* %arg1) {
entry:
  %0 = bitcast %Array* %arg1 to %"struct.quantum::Array"*
  call void @array_unreference(%"struct.quantum::Array"* %0)
  ret void
}

declare void @array_unreference(%"struct.quantum::Array"*)

define i1 @__quantum__rt__result_equal(%Result* %arg1, %Result* %arg2) {
entry:
  %0 = bitcast %Result* %arg1 to %class.RESULT*
  %1 = bitcast %Result* %arg2 to %class.RESULT*
  %2 = call i1 @result_equal(%class.RESULT* %0, %class.RESULT* %1)
  ret i1 %2
}

declare i1 @result_equal(%class.RESULT*, %class.RESULT*)

define void @__quantum__rt__result_unreference(%Result* %arg1) {
entry:
  %0 = bitcast %Result* %arg1 to %class.RESULT*
  call void @result_unreference(%class.RESULT* %0)
  ret void
}

declare void @result_unreference(%class.RESULT*)

define %Array* @__quantum__rt__array_copy(%Array* %arg1) {
entry:
  %0 = bitcast %Array* %arg1 to %"struct.quantum::Array"*
  %1 = call %"struct.quantum::Array"* @array_copy(%"struct.quantum::Array"* %0)
  %2 = bitcast %"struct.quantum::Array"* %1 to %Array*
  ret %Array* %2
}

declare %"struct.quantum::Array"* @array_copy(%"struct.quantum::Array"*)

define void @__quantum__rt__array_reference(%Array* %arg1) {
entry:
  %0 = bitcast %Array* %arg1 to %"struct.quantum::Array"*
  call void @array_reference(%"struct.quantum::Array"* %0)
  ret void
}

declare void @array_reference(%"struct.quantum::Array"*)

define %TupleHeader* @__quantum__rt__tuple_create(i64 %arg1) {
entry:
  %0 = call %"struct.quantum::TupleHeader"* @tuple_create(i64 %arg1)
  %1 = bitcast %"struct.quantum::TupleHeader"* %0 to %TupleHeader*
  ret %TupleHeader* %1
}

declare %"struct.quantum::TupleHeader"* @tuple_create(i64)

define void @__quantum__rt__string_reference(%String* %arg1) {
entry:
  call void @string_reference(%String* %arg1)
  ret void
}

declare void @string_reference(%String*)
