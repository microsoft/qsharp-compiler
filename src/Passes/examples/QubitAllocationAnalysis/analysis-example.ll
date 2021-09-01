; ModuleID = 'qir/TeleportChain.ll'
source_filename = "qir/TeleportChain.ll"

%Qubit = type opaque
%String = type opaque

define internal fastcc i64 @TeleportChain__Calculate__body(i64 %n) unnamed_addr {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %.not = icmp eq i64 %n, 0
  br i1 %.not, label %continue__1, label %then0__1

then0__1:                                         ; preds = %entry
  %0 = add i64 %n, -1
  %1 = call fastcc i64 @TeleportChain__Calculate__body(i64 %0)
  %2 = add i64 %1, 2
  br label %continue__1

continue__1:                                      ; preds = %entry, %then0__1
  %ret.0 = phi i64 [ %2, %then0__1 ], [ 2, %entry ]
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  ret i64 %ret.0
}

declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

define internal fastcc i64 @TeleportChain__Main__body() unnamed_addr {
entry:
  %0 = call fastcc i64 @TeleportChain__Calculate__body(i64 4)
  ret i64 %0
}

define i64 @TeleportChain__Main__Interop() local_unnamed_addr #0 {
entry:
  %0 = call fastcc i64 @TeleportChain__Main__body()
  ret i64 %0
}

define void @TeleportChain__Main() local_unnamed_addr #1 {
entry:
  %0 = call fastcc i64 @TeleportChain__Main__body()
  %1 = call %String* @__quantum__rt__int_to_string(i64 %0)
  call void @__quantum__rt__message(%String* %1)
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
