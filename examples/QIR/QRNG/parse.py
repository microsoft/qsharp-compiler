from  pprint import pprint
import llvmlite.binding as llvm
import re
import argparse
import sys

def getBlkId(blkIds,blkNam):
    blkNam = blkNam.replace("v_","")
    if blkNam not in blkIds: 
        blkIds[blkNam] = len(blkIds)
    return blkIds[blkNam]

def eqlPart(str1,str2):
    return (str2 == str1[:len(str2)])

def cleanArg(arg):
    return  arg.replace(')','').replace('(','')

def parseIns(blkIds,ins):
    tkns = re.sub(r'%(\d+)',r'v[\1]',str(ins))
    tkns = re.sub("[,@%.]","",tkns)
    tkns = tkns.split()
    print(f"                             ////// {tkns} [{ins.opcode}]")
    if ins.opcode == 'call':
        if tkns[1] == '=':
            if eqlPart(tkns[4],'__quantum__qis__measure'):
                if tkns[0][:2] == 'v[': decl = ""
                else:                   decl = "int "
                print(f'        {decl}{tkns[0]} = rand() & 0x800 == 0x800 ? 1 : 0;  // Return a random bit')
            elif eqlPart(tkns[4],'Qrng'):
                if tkns[3] == "Array*": p = "p"
                else:                   p = ""
                if tkns[0][:2] == 'v[': decl = ""
                else:                   decl = "int "
                print(f'      {decl}{p}{tkns[0]} = {tkns[4]};')
            elif eqlPart(tkns[4],'__quantum__rt__array_create_1d'):
                if tkns[0][:2] == 'v[': decl = ""
                else:                   decl = "int * "
                print(f'      {decl}p{tkns[0]} = (int*)malloc(sizeof(int)*{cleanArg(tkns[7])});')
            elif eqlPart(tkns[4],'__quantum__rt__result_equal') and tkns[5] == 'oneBit':
                print(f'      {tkns[0]} = {cleanArg(tkns[5])} == {cleanArg(tkns[7])} ? 1 : 0;')
            elif eqlPart(tkns[4],'__quantum__rt__array_get_element_ptr_1d'):
                print(f'      p{tkns[0]} = &p{cleanArg(tkns[5])}[{cleanArg(tkns[7])}];')
            elif eqlPart(tkns[4],'__quantum__rt__array_copy'):
                print(f'      p{tkns[0]} = p{cleanArg(tkns[5])};')
            elif eqlPart(tkns[4],'__quantum__rt__qubit_allocate'):
                print(f'      int {tkns[0]};')
            else:
                pass
        else:
            pass #print(f'        int {tkns[0]} = 0;')
    elif ins.opcode == 'ret':
        if tkns[1][-6:] == "Array*" or tkns[1][-7:] == 'Array"*': p = "p"
        else:                        p = ""
        print(f'      return {p}{tkns[2]};')

    elif ins.opcode == "alloca":
        if tkns[3][-1:] == "*": op = "int* p"
        else:                   op = "int "
        print(f'      {op}{ins.name}; //alloca')

    elif ins.opcode == "store":
        hack = False
        for op in ins.operands:     
            if 'Qrng__RandomInt__body' in str(op): hack = True
        if hack:
            print(f'      *p{tkns[4]} = {tkns[2]}; //store hack!!!')
        else:
            if tkns[1] == "Array*": p = "p"
            else:                   p = ""
            print(f'      {p}{tkns[4]} = {p}{tkns[2]}; //store')

    elif ins.opcode == "load":
        if tkns[3] == "Array*": p = "p"
        else:                   p = ""
        print(f'      {p}{tkns[0]} = {p}{tkns[5]}; //load')

    elif ins.opcode == "bitcast":
        if tkns[3][-1:] == "*": op = "p"
        else:                   op = ""
        print(f'      {op}{tkns[0]} = {op}{tkns[4]}; //bitcast')

    elif ins.opcode == "br":
        if tkns[1] == 'label':
            print(f'      blkPrv = blkCur; blkCur = {getBlkId(blkIds,tkns[2])};')
        else:
            print(f'      blkPrv = blkCur; blkCur = {tkns[2]} ? {getBlkId(blkIds,tkns[4])} : {getBlkId(blkIds,tkns[6])};')

    elif ins.opcode == "icmp":
        if tkns[3] == 'sge'   : cmp = '>='
        elif tkns[3] == 'sle' : cmp = '<='
        elif tkns[3] == 'slt' : cmp = '<'
        elif tkns[3] == 'sgt' : cmp = '>'
        else: cmp = '???'
        print(f'      {tkns[0]} = {tkns[5]} {cmp} {tkns[6]} ? 1 : 0; //icmp')

    elif ins.opcode == "select":
        tst = tkns[4].replace("true","1").replace("false","0")
        print(f'      {tkns[0]} = {tst} ? {tkns[6]} : {tkns[8]}; //select')

    elif ins.opcode == 'phi':
        print(f'      int {tkns[0]} = blkPrv == {getBlkId(blkIds,tkns[6])} ? {tkns[5]} : {tkns[9]}; //phi')

    elif ins.opcode == 'add':
        print(f'      {tkns[0]} = {tkns[4]} + {tkns[5]}; ')

    elif ins.opcode == 'shl':
        print(f'      {tkns[0]} = {tkns[4]} << {tkns[5]};')
    else:
        pass #print(f'            ### INSTR n={ins.name} o={ins.opcode} r={tkns}')

def parseBlock(blkIds,blk):
    print(f'  case {getBlkId(blkIds,blk.name)}:')
    print(f'    ;')
    for ins in blk.instructions:
        parseIns(blkIds,ins)
    print('      break;')

def parseFunc(func):
    blkIds = {}

    if "Array" in str(func.type):   rtnTyp = "int*"
    else:                           rtnTyp = "int"
    print(f'{rtnTyp} {func.name}() {{')
    blkIds = {}
    print('  int   blkPrv = 0;')
    print('  int   blkCur = 0;')
    print('  int   v[20];')
    print('  int*  pv[20];')
    print('  while (1) switch (blkCur) {')
    for blk in func.blocks:
        parseBlock(blkIds,blk)
    print('  }')
    print('}')


def parseFile(mod,doRTT,doHost):
    if (doRTT):
        print('#include "synopsys_gpio.h"')
        print('#include "ARMCM4_FP.h"')
        print('#include "SEGGER_RTT.h"')
        print('')
    print('#include <stdio.h>')
    print('#include <stdlib.h>')
    print('')
    print('int PauliX   = 0;')
    print('int PauliZ   = 1;')
    print('int ResultOne= 1;')
    print('int EXE_RESULT[32];')
    print('int dummy     =0;')
    print('')
    print('void sleep(int secs) {')
    print('    for (int j=0; j<secs; j++)')
    if (doHost):    print('        for (int i=0; i<200000000; i++)')
    else:           print('        for (int i=0; i<2000000; i++)')
    print('            dummy += i % 97;')
    print('}')
    print('')
    for func in mod.functions:
        if func.name.startswith('Qrng'):
            parseFunc(func)
            for att in func.attributes:
                if att == b'"EntryPoint"':
                    print('')
                    print('int main() {')
                    if (doRTT):
                        print('    // initialize the JTAG printing library')
                        print('    SEGGER_RTT_Init();')
                        print('    ')
                        print('    GPIO0->SWPORTA_DR = 0;')
                        print('    GPIO0->SWPORTA_DDR = 0xFF;')
                        print('    ')
                    print('    for (int i=0; i<32; i++) EXE_RESULT[i] = -1;')
                    print('    while (1) {')
                    if (doRTT):
                        print('      for (int i=0; i<32; i++) ')
                        print('          SEGGER_RTT_printf(0,"%2d = %08x\\n",i,EXE_RESULT[i]);')
                        print('      while (1)')
                        print('      {')
                        print('          // Toggle GPIO0')
                        print('          GPIO0->SWPORTA_DR = 0x00;')
                        print('          GPIO0->SWPORTA_DR = 0x01;')
                        print('      }')
                    if (doHost):
                        print('      for (int i=0; i<32; i++) ')
                        print('          printf("%2d = %08x\\n",i,EXE_RESULT[i]);')
                    print('      sleep(10);')
                    print(f'      int* rslt = {func.name}();')
                    print('      for (int i=0; i<32; i++) ')
                    print('          EXE_RESULT[i] = rslt[i];')
                    print('  }')
                    print('}')

def load(inp):
    with open(inp,"r") as file:
        text = file.read()

    llvm.initialize()
    llvm.initialize_all_targets()
    llvm.initialize_all_asmprinters()

#    target          = llvm.Target.from_triple("x86_64")
#    target_machine  = target.create_target_machine()
#    backing_mod     = llvm.parse_assembly("")
#    engine          = llvm.create_mcjit_compiler(backing_mod, target_machine)
    mod             = llvm.parse_assembly(text)
#    mod.verify()
#    engine.add_module(mod)
#    engine.finalize_object()
#    engine.run_static_constructors()
#    assembly        = target_machine.emit_assembly(mod)
    return mod

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("-b", "--baseName", default="QRNG",
                    help="Base Name for .ll input and .c output")
    parser.add_argument("-r", "--rtt", action="store_true",
                    help="add RTT support")
    parser.add_argument("-p", "--printf", action="store_true",
                    help="do printf when running on host")
    args = parser.parse_args()

    inpNam = args.baseName + ".ll"
    outNam = args.baseName + ".c"

    print(f'Generating: {outNam}')
    origStdout = sys.stdout
    with open(outNam,"w") as out:
        sys.stdout = out
        mod = load(inpNam)
        parseFile(mod,args.rtt,args.printf)
    sys.stdout = origStdout

if __name__ == '__main__':
    main()
