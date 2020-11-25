from  pprint import pprint
import llvmlite.binding as bnd

with open("QRNG.ll","r") as file:
    text = file.read()
print(text)

mod = bnd.parse_assembly(text)

for func in mod.functions:
    if func.name.startswith('Qrng'):
        print(f'### NAME={func.name}')
        for blk in func.blocks:
            print(f'    ### BLOCK {blk.name}')
            for ins in blk.instructions:
                print(f'        ### INSTR n={ins.name} o={ins.opcode} r={ins}')
                #for op in ins.operands:
                #    print(f'          ### OP n={op.name} r={op} ')
                
print('DONE')