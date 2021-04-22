define { i1, i1 }* @Microsoft__Quantum__Testing__QIR__TestShortCircuiting__body() {
entry:
  %0 = call i1 @__quantum__qis__getrandombool__body(i64 1)
  br i1 %0, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %entry
  %1 = call i1 @__quantum__qis__getrandombool__body(i64 2)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %entry
  %rand = phi i1 [ %1, %condTrue__1 ], [ %0, %entry ]
  %2 = call i1 @__quantum__qis__getrandombool__body(i64 3)
  br i1 %2, label %condContinue__2, label %condFalse__1

condFalse__1:                                     ; preds = %condContinue__1
  %3 = call i1 @__quantum__qis__getrandombool__body(i64 4)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__1, %condContinue__1
  %ror = phi i1 [ %2, %condContinue__1 ], [ %3, %condFalse__1 ]
  %4 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1* getelementptr (i1, i1* null, i32 1) to i64), i64 2))
  %5 = bitcast %Tuple* %4 to { i1, i1 }*
  %6 = getelementptr inbounds { i1, i1 }, { i1, i1 }* %5, i32 0, i32 0
  %7 = getelementptr inbounds { i1, i1 }, { i1, i1 }* %5, i32 0, i32 1
  store i1 %rand, i1* %6, align 1
  store i1 %ror, i1* %7, align 1
  ret { i1, i1 }* %5
}
