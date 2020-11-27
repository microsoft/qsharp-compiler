from  pprint import pprint
import llvmlite.binding as llvm
import re

reCall1  = re.compile(r'\s*(\S+) = +call (\S+) @([^(]+)\(([^)]*)\)')
reCall2  = re.compile(r'\s*call void @([^(]+)\(([^)]*)\)')
reRet1   = re.compile(r'\s*ret (\S+) (\S+)')
reStore1 = re.compile(r'\s*store (\S+) (\S+), (\S+) (\S+)')
reBr1    = re.compile(r'\s*br label (\S+)')
reBr2    = re.compile(r'\s*br i1 (\S+), label (\S+), label (\S)+')
reCmp1   = re.compile(r'\s*(\S+) = icmp (\S+) (\S+) (\S+), (\S+)')
reSel1   = re.compile(r'\s*(\S+) = select i1 (\S+), i1 (\S+), i1 (\S+)')
reLoad1  = re.compile(r'\s*(\S+) = load (\S+), (\S+) (\S+)')

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

def parseIns(ins):
    line = str(ins).replace("%","v_")
    if ins.opcode == 'call':
        m1 = reCall1.match(line)
        m2 = reCall2.match(line)
        if m1 is not None:
            if m1.group(3) == '__quantum__qis__measure':
                print(f'        int {m1.group(1)} = 1;')
        elif m2 is not None:
            print(f'            ### CALL_2 1={m2.group(1)} 2={m2.group(2)}')
        else:
            print(f'            ### call {line}')
    elif ins.opcode == 'ret':
        m1 = reRet1.match(line)
        if m1 is not None:
            print(f'        return {m1.group(2)};')
        else:
            print(f'            ### ret {line}')

    elif ins.opcode == "alloca":
        print(f'        int v_{ins.name};')

    elif ins.opcode == "store":
        m1 = reStore1.match(line)
        if m1 is not None:
            print(f'        {m1.group(4)} = {m1.group(2)};')
        else:
            print(f'            ### store {line}')

    elif ins.opcode == "load":
        m1 = reLoad1.match(line)
        if m1 is not None:
            print(f'        int {m1.group(1)} = {m1.group(4)};')
        else:
            print(f'            ### load  {line}')

    elif ins.opcode == "br":
        m1 = reBr1.match(line)
        m2 = reBr2.match(line)
        if m1 is not None:
            print(f'        blkNam = "{m1.group(1)}";')
        elif m2 is not None:
            print(f'        blkName = {m2.group(1)} ? "{m2.group(2)}" : "{m2.group(3)}";')
        else:
            print(f'            ### br {line}')
    elif ins.opcode == "icmp":
        m1 = reCmp1.match(line)
        if m1 is not None:
            if m1.group(2) == 'sge': cmp = '>='
            elif m1.group(2) == 'sle' : cmp = '<='
            else: cmp = '???'
            print(f'        int {m1.group(1)} = {m1.group(4)} {cmp} {m1.group(5)};')
        else:
            print(f'            ### icmp {line}')

    elif ins.opcode == "select":
        m1 = reSel1.match(line)
        if m1 is not None:
            print(f'        int {m1.group(1)} = {m1.group(2)} ? {m1.group(3)} : {m1.group(4)}')
        else:
            print(f'            ### select {line}')

    else:
        print(f'            ### INSTR n={ins.name} o={ins.opcode} r={line}')

def parseBlock(blk):
    print(f'  case "v_{blk.name}"":')
    for ins in blk.instructions:
        parseIns(ins)
    print('    break;')

def parseFunc(func):
    print(f'void {func.name}() {{')
    print('  char* blkNam = "v_entry";')
    print('  while (1) switch blkNam {')
    for blk in func.blocks:
        parseBlock(blk)
    print('  }')
    print('}')

for func in mod.functions:
    if func.name.startswith('Qrng'):
        parseFunc(func)
                
print('DONE')
