$QSC = "..\..\..\src\QsCompiler\CommandLineTool\bin\Debug\netcoreapp3.1\qsc.exe"

&$QSC build --qir s --build-exe --input QRNG.qs ..\QirCore.qs ..\QirTarget.qs --proj QRNG
