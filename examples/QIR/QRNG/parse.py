from  pprint import pprint
import llvmlite.binding as llvm
import re

with open("QRNG.ll","r") as file:
    text = file.read()
#print(text)


llvm.initialize()
llvm.initialize_native_target()
llvm.initialize_native_asmprinter()

target = llvm.Target.from_default_triple()
target_machine = target.create_target_machine()
backing_mod = llvm.parse_assembly("")
engine = llvm.create_mcjit_compiler(backing_mod, target_machine)
mod = llvm.parse_assembly(text)
mod.verify()
engine.add_module(mod)
engine.finalize_object()
engine.run_static_constructors()
assembly = target_machine.emit_assembly(mod)

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
    tkns = str(ins).replace("%","v_").replace(",","").replace("@","").replace("_.","_").split()
    #print(f"                        ............... {tkns} [{ins.opcode}]")
    if ins.opcode == 'call':
        if tkns[1] == '=':
            if eqlPart(tkns[4],'__quantum__qis__measure'):
                print(f'      int {tkns[0]} = 1;')
            elif eqlPart(tkns[4],'Qrng'):
                print(f'      int {tkns[0]} = {tkns[4]};')
            elif eqlPart(tkns[4],'__quantum__rt__array_create_1d'):
                print(f'      int {tkns[0]}[{cleanArg(tkns[7])}];')
            elif eqlPart(tkns[4],'__quantum__rt__result_equal') and tkns[5] == 'v_oneBit':
                print(f'      int {tkns[0]} = {cleanArg(tkns[7])};')
            elif eqlPart(tkns[4],'__quantum__rt__array_get_element_ptr_1d'):
                print(f'      int* {tkns[0]} = {cleanArg(tkns[5])};')
            elif eqlPart(tkns[4],'__quantum__rt__array_copy'):
                print(f'      int* {tkns[0]} = {cleanArg(tkns[5])};')
            elif eqlPart(tkns[4],'__quantum__rt__qubit_allocate'):
                print(f'      int* {tkns[0]} = 0;')
            else:
                pass
        else:
            pass #print(f'        int {tkns[0]} = 0;')
    elif ins.opcode == 'ret':
        print(f'      return {tkns[2]};')

    elif ins.opcode == "alloca":
        if tkns[3][-1:] == "*": op = "int*"
        else:                   op = "int"
        print(f'      {op} v_{ins.name}; //alloca')

    elif ins.opcode == "store":
        print(f'      {tkns[4]} = {tkns[2]}; //store')

    elif ins.opcode == "load":
        print(f'      int {tkns[0]} = {tkns[5]}; //load')

    elif ins.opcode == "bitcast":
        if tkns[3][-1:] == "*": op = "int*"
        else:                   op = "int"
        print(f'      {op} {tkns[0]} = {tkns[4]}; //bitcast')

    elif ins.opcode == "br":
        if tkns[1] == 'label':
            print(f'      blkPrv = blkCur; blkCur = {getBlkId(blkIds,tkns[2])};')
        else:
            print(f'      blkPrv = blkCur; blkCur = {tkns[2]} ? {getBlkId(blkIds,tkns[4])} : {getBlkId(blkIds,tkns[6])};')

    elif ins.opcode == "icmp":
        if tkns[3] == 'sge': cmp = '>='
        elif tkns[3] == 'sle' : cmp = '<='
        else: cmp = '???'
        print(f'      int {tkns[0]} = {tkns[5]} {cmp} {tkns[6]} ? 1 : 0; //icmp')

    elif ins.opcode == "select":
        tst = tkns[4].replace("true","1").replace("false","0")
        print(f'      int {tkns[0]} = {tst} ? {tkns[6]} : {tkns[8]}; //select')

    elif ins.opcode == 'phi':
        print(f'      int {tkns[0]} = blkPrv == {getBlkId(blkIds,tkns[6])} ? {tkns[5]} : {tkns[9]}; //phi')

    elif ins.opcode == 'add':
        print(f'      int {tkns[0]} = {tkns[4]} + {tkns[5]}; ')

    elif ins.opcode == 'shl':
        print(f'      int {tkns[0]} = {tkns[4]} << {tkns[5]};')
    else:
        pass #print(f'            ### INSTR n={ins.name} o={ins.opcode} r={tkns}')

def parseBlock(blkIds,blk):
    print(f'  case {getBlkId(blkIds,blk.name)}:')
    for ins in blk.instructions:
        parseIns(blkIds,ins)
    print('      break;')

def parseFunc(func):
    blkIds = {}

    if "Array" in str(func.type):   rtnTyp = "int*"
    else:                           rtnTyp = "int"
    print(f'{rtnTyp} {func.name}() {{')
    blkIds = {}
    print('  int blkPrv = 0;')
    print('  int blkCur = 0;')
    print('  while (1) switch (blkCur) {')
    for blk in func.blocks:
        parseBlock(blkIds,blk)
    print('  }')
    print('}')

print('int PauliX   = 0;')
print('int PauliZ   = 1;')
print('int ResultOne= 1;')
print('int* EXE_RESULT;')
print('')
for func in mod.functions:
    if func.name.startswith('Qrng'):
        parseFunc(func)
        for att in func.attributes:
            if att == b'"EntryPoint"':
                print('')
                print('int main() {')
                print(f'    EXE_RESULT = {func.name}();')
                print('}')
                