define i64 @Microsoft__Quantum__Testing__QIR__TestWhile__body(i64 %a, i64 %b) {
entry:
  %n = alloca i64
  store i64 %a, i64* %n
  br label %while__1

while__1:                                         ; preds = %do__1, %entry
  %0 = load i64, i64* %n
  %1 = icmp slt i64 %0, %b
  br i1 %1, label %do__1, label %wend__1

do__1:                                            ; preds = %while__1
  %2 = mul i64 %0, 2
  store i64 %2, i64* %n
  br label %while__1

wend__1:                                          ; preds = %while__1
  %3 = load i64, i64* %n
  ret i64 %3
}
