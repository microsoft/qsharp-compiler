; ModuleID = 'qir/ConstSizeArray.ll'
source_filename = "qir/ConstSizeArray.ll"

%Array = type opaque
%Qubit = type opaque

define internal fastcc void @Feasibility__QubitMapping__body() unnamed_addr {
entry:
  %qs = call %Array* @__quantum__rt__qubit_allocate_array(i64 3)
  call void @__quantum__rt__array_update_alias_count(%Array* %qs, i32 1)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qs, i64 0)
  %1 = bitcast i8* %0 to %Qubit**
  %qubit = load %Qubit*, %Qubit** %1, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit)
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qs, i64 1)
  %3 = bitcast i8* %2 to %Qubit**
  %qubit.1 = load %Qubit*, %Qubit** %3, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit.1)
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qs, i64 2)
  %5 = bitcast i8* %4 to %Qubit**
  %qubit.2 = load %Qubit*, %Qubit** %5, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit.2)
  call void @__quantum__rt__array_update_alias_count(%Array* %qs, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qs)
  ret void
}

declare %Array* @__quantum__rt__qubit_allocate_array(i64) local_unnamed_addr

declare void @__quantum__rt__qubit_release_array(%Array*) local_unnamed_addr

declare void @__quantum__rt__array_update_alias_count(%Array*, i32) local_unnamed_addr

declare i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64) local_unnamed_addr

declare void @__quantum__qis__x__body(%Qubit*) local_unnamed_addr

define void @Feasibility__QubitMapping__Interop() local_unnamed_addr #0 {
entry:
  call fastcc void @Feasibility__QubitMapping__body()
  ret void
}

define void @Feasibility__QubitMapping() local_unnamed_addr #1 {
entry:
  call fastcc void @Feasibility__QubitMapping__body()
  ret void
}

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
