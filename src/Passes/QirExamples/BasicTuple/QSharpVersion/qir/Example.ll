
%Range = type { i64, i64, i64 }
%Tuple = type opaque
%String = type opaque

@PauliI = internal constant i2 0
@PauliX = internal constant i2 1
@PauliY = internal constant i2 -1
@PauliZ = internal constant i2 -2
@EmptyRange = internal constant %Range { i64 0, i64 1, i64 -1 }

define internal i64 @ConstArrayReduction__Main__body() {
entry:
  %0 = call { i64, i64 }* @ConstArrayReduction__TupleTest__body()
  %1 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %0, i32 0, i32 0
  %x = load i64, i64* %1, align 4
  %2 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %0, i32 0, i32 1
  %y = load i64, i64* %2, align 4
  %3 = mul i64 %x, %y
  %4 = bitcast { i64, i64 }* %0 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %4, i32 -1)
  ret i64 %3
}

define internal { i64, i64 }* @ConstArrayReduction__TupleTest__body() {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %1 = bitcast %Tuple* %0 to { i64, i64 }*
  %2 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %1, i32 0, i32 1
  store i64 5, i64* %2, align 4
  store i64 3, i64* %3, align 4
  ret { i64, i64 }* %1
}

declare void @__quantum__rt__tuple_update_reference_count(%Tuple*, i32)

declare %Tuple* @__quantum__rt__tuple_create(i64)

define i64 @ConstArrayReduction__Main__Interop() #0 {
entry:
  %0 = call i64 @ConstArrayReduction__Main__body()
  ret i64 %0
}

define void @ConstArrayReduction__Main() #1 {
entry:
  %0 = call i64 @ConstArrayReduction__Main__body()
  %1 = call %String* @__quantum__rt__int_to_string(i64 %0)
  call void @__quantum__rt__message(%String* %1)
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*)

declare %String* @__quantum__rt__int_to_string(i64)

declare void @__quantum__rt__string_update_reference_count(%String*, i32)

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
