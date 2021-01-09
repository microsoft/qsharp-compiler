gccARM = arm-none-eabi-gcc --std=c99 -Wextra -ffunction-sections -fdata-sections -g -O2 -mthumb -mcpu=cortex-m4 -mfloat-abi=hard -mfpu=fpv4-sp-d16 -Wall -D ARMCM4_FP -W -I ./CMSIS/CMSIS/Core/Include/ -I ./CMSIS/Device/ARM/ARMCM4/Include -I ./CSR_Headers/ -I ./drivers -I ./RTT

all: QStoLLVM LLVMtoX86 LLVMtoARM X86toEXE ARMtoELF ELFtoBIN GetOutBuf CopytoFS

QStoLLVM:
	dotnet run --project ../../../src/QsCompiler/CommandLineTool/ build --qir s --build-exe --input ./QRNG.qs ../QirCore.qs ../QirTarget.qs --proj QRNG
	@pwsh.exe -Command "gci | sort -Property LastWriteTime -Descending | select -First 3 | format-table -HideTableHeaders"

LLVMtoX86:
	clang -S -fno-addrsig -o QRNG_x86.s QRNG.ll 
	@pwsh.exe -Command "gci | sort -Property LastWriteTime -Descending | select -First 3 | format-table -HideTableHeaders"

LLVMtoARM:
	clang -S -fno-addrsig -o QRNG_ARM.s -target thumbv7m-none-unknown-eabi -mfloat-abi=hard QRNG.ll
	@pwsh.exe -Command "gci | sort -Property LastWriteTime -Descending | select -First 3 | format-table -HideTableHeaders"

X86toEXE:
	gcc -o QRNG.exe -DDOHOST QRTstubs.c QRNG_x86.s
	@pwsh.exe -Command "gci | sort -Property LastWriteTime -Descending | select -First 3 | format-table -HideTableHeaders"

ARMtoELF:
	$(gccARM) -c drivers/csi.c -o csi.o
	$(gccARM) -c drivers/hcl.c -o hcl.o
	$(gccARM) -c QRNG_ARM.s -mfloat-abi=hard -mfpu=fpv4-sp-d16 -o QRNG.o
	$(gccARM) -c QRTstubs.c -o QRTstubs.o
	$(gccARM) -c CMSIS/Device/ARM/ARMCM4/Source/system_ARMCM4.c -o system_ARMCM4.o
	$(gccARM) -c drivers/acl.c -o acl.o
	$(gccARM) -c RTT/SEGGER_RTT.c -o SEGGER_RTT.o
	$(gccARM) -c CMSIS/Device/ARM/ARMCM4/Source/startup_ARMCM4.c -o startup_ARMCM4.o
	$(gccARM) -c QRTstubs.c -o QRTstubs.o
	$(gccARM) -c RTT/SEGGER_RTT_printf.c -o SEGGER_RTT_printf.o
	arm-none-eabi-gcc '-O2' '-mthumb' '-mcpu=cortex-m4' '-mfloat-abi=hard' '-mfpu=fpv4-sp-d16'  '-T' ./CMSIS/Device/ARM/ARMCM4/Source/GCC/gcc_arm.ld '-specs=nosys.specs' '-ffunction-sections' '-fdata-sections' '-Wl,--gc-sections' QRTstubs.o QRNG.o startup_ARMCM4.o system_ARMCM4.o acl.o csi.o hcl.o SEGGER_RTT.o SEGGER_RTT_printf.o '-o' QRNG.elf
	pwsh.exe -Command "gci | sort -Property LastWriteTime -Descending | select -First 3 | format-table -HideTableHeaders"

ELFtoBIN:
	cmd /c arm-none-eabi-objcopy -O binary QRNG.elf QRNG.bin 
	pwsh.exe -Command "gci | sort -Property LastWriteTime -Descending | select -First 3 | format-table -HideTableHeaders"

GetOutBuf:
	pwsh.exe -Command "arm-none-eabi-objdump -Ds QRNG.elf | findstr '_SEGGER_RTT EXE_RESULT'"

CopyToFS:
	pwsh.exe -Command "copy 'QRNG*.*' '~/Onedrive/Reilly Lab'"
	pwsh.exe -Command "copy 'QRTstubs.c' '~/Onedrive/Reilly Lab'"  
