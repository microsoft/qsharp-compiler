from  pprint import pprint
import llvmlite.binding as bnd
import re

reCall1  = re.compile(r'\s*(\S+) = +call (\S+) @([^(]+)\(([^)]*)\)')
reCall2  = re.compile(r'\s*call void @([^(]+)\(([^)]*)\)')
reRet1   = re.compile(r'\s*ret (\S+) (\S+)')
reStore1 = re.compile(r'\s*store (\S+) (\S+), (\S+) (\S+)')
reBr1    = re.compile(r'\s*br label (\S+)')

with open("QRNG.ll","r") as file:
    text = file.read()
#print(text)

mod = bnd.parse_assembly(text)

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
            print(f'            ### ret {line}')

    elif ins.opcode == "br":
        m1 = reBr1.match(line)
        if m1 is not None:
            print(f'        blkNam = "{m1.group(1)}";')
        else:
            print(f'            ### ret {line}')

    else:
        print(f'            ### INSTR n={ins.name} o={ins.opcode} r={line}')

def parseBlock(blk):
    print(f'  case "{blk.name}"":')
    for ins in blk.instructions:
        parseIns(ins)
    print('    break;')

def parseFunc(func):
    print(f'void {func.name}() {{')
    print('  char* blkNam = "entry";')
    print('  while (1) switch blkNam {')
    for blk in func.blocks:
        parseBlock(blk)
    print('  }')
    print('}')

for func in mod.functions:
    if func.name.startswith('Qrng'):
        parseFunc(func)
                
print('DONE')