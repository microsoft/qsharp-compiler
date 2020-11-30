$gccARM = "arm-none-eabi-gcc --std=c99 -Wextra -ffunction-sections -fdata-sections -g -O2 -mthumb -mcpu=cortex-m4 -mfloat-abi=hard -mfpu=fpv4-sp-d16 -Wall -D ARMCM4_FP -W -I .\CMSIS\CMSIS\Core\Include\ -I .\CMSIS\Device\ARM\ARMCM4\Include -I .\CSR_Headers\ -I .\drivers -I .\RTT"

"Create LLVM QIR file from Q# source: (QRNG.ll)"
dotnet run --project ..\..\..\src\QsCompiler\CommandLineTool\ build --qir s --build-exe --input .\QRNG.qs ..\QirCore.qs ..\QirTarget.qs --proj QRNG
"Convert LLVM QIR file to X86 Assembler: (QRNG_x86.s)"
clang -S -fno-addrsig -o QRNG_x86.s QRNG.ll 
"Convert LLVM QIR file to ARM Assembler: (QRNG_ARM.s)"
clang -S -fno-addrsig -o QRNG_ARM.s -target "arm-cortex-m4-eabi" QRNG.ll 
"Create X86 executable (QRNG.exe):"
gcc -o QRNG.exe -DDOHOST QRTstubs.c QRNG_x86.s
"Create ARM executable (QRNG.bin, QRNG.elf):"
iex "$gccARM -c drivers\csi.c -o csi.o"
iex "$gccARM -c drivers\hcl.c -o hcl.o"
iex "$gccARM -c QRNG_ARM.s -mfloat-abi=hard -mfpu=fpv4-sp-d16 -o QRNG.o"
iex "$gccARM -c QRTstubs.c -o QRTstubs.o"
iex "$gccARM -c CMSIS\Device\ARM\ARMCM4\Source\system_ARMCM4.c -o system_ARMCM4.o"
iex "$gccARM -c drivers\acl.c -o acl.o"
iex "$gccARM -c RTT\SEGGER_RTT.c -o SEGGER_RTT.o"
iex "$gccARM -c CMSIS\Device\ARM\ARMCM4\Source\startup_ARMCM4.c -o startup_ARMCM4.o"
iex "$gccARM -c QRTstubs.c -o QRTstubs.o"
iex "$gccARM -c RTT\SEGGER_RTT_printf.c -o SEGGER_RTT_printf.o"
arm-none-eabi-gcc '-O2' '-mthumb' '-mcpu=cortex-m4' '-mfloat-abi=hard' '-mfpu=fpv4-sp-d16'  '-T' .\CMSIS\Device\ARM\ARMCM4\Source\GCC\gcc_arm.ld '-specs=nosys.specs' '-ffunction-sections' '-fdata-sections' '-Wl,--gc-sections' QRTstubs.o QRNG.o startup_ARMCM4.o system_ARMCM4.o acl.o csi.o hcl.o SEGGER_RTT.o SEGGER_RTT_printf.o '-o' QRNG.elf

cmd /c arm-none-eabi-objcopy -O binary QRNG.elf QRNG.bin 
arm-none-eabi-objdump -Ds QRNG.elf | findstr "_SEGGER_RTT EXE_RESULT"