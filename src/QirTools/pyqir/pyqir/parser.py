# Copyright(c) Microsoft Corporation.
# Licensed under the MIT License.

from .pyqir import *
from . import export
from typing import List, Optional, Tuple

try:
    import retworkx as rx
except ImportError:
    rx = None

try:
    import retworkx.visualization as rxv
except ImportError:
    rxv = None

try:
    import qiskit as qk
except ImportError:
    qk = None

try:
    import qutip as qt
    import qutip.qip.circuit
    import qutip.qip.operations
except ImportError:
    qt = None

class QirType:
    """
    Instances of QirType represent a type description in QIR. Specific subclasses may contain
    additional properties of that type.
    """
    def __new__(cls, ty: PyQirType):
        if ty.is_qubit:
            return super().__new__(QirQubitType)
        elif ty.is_result:
            return super().__new__(QirResultType)
        elif ty.is_void:
            return super().__new__(QirVoidType)
        elif ty.is_integer:
            return super().__new__(QirIntegerType)
        elif ty.is_pointer:
            return super().__new__(QirPointerType)
        elif ty.is_double:
            return super().__new__(QirDoubleType)
        elif ty.is_array:
            return super().__new__(QirArrayType)
        elif ty.is_struct:
            return super().__new__(QirStructType)
        elif ty.is_named_struct:
            return super().__new__(QirNamedStructType)
        else:
            return super().__new__(cls)

    def __init__(self, ty: PyQirType):
        self.ty = ty

class QirVoidType(QirType):
    """
    Instances of QirVoidType represent a void type in QIR.
    """
    pass

class QirIntegerType(QirType):
    """
    Instances of QirIntegerType represent a signed integer in QIR. Note that there is no unsigned
    integer type, just unsigned arithmetic instructions.
    """

    @property
    def width(self) -> int:
        """
        Gets the bit width of this integer type.
        """
        return self.ty.integer_width

class QirPointerType(QirType):
    """
    Instances of QirPointerType represent a pointer to some other type in QIR.
    """

    @property
    def type(self) -> QirType:
        """
        Gets the QirType this to which this pointer points.
        """
        return QirType(self.ty.pointer_type)

    @property
    def addrspace(self):
        """
        Gets the address space to which this pointer points.
        """
        return self.ty.pointer_addrspace

class QirDoubleType(QirType):
    """
    Instances of QirDoubleType represent the double-sized floating point type in a QIR program.
    """
    pass

class QirArrayType(QirType):
    """
    Instances of the QirArrayType represent the native LLVM fixed-length array type in a QIR program.
    """

    @property
    def element_types(self) -> List[QirType]:
        """
        Gets the ordered list of QirTypes representing the underlying array types.
        """
        return [QirType(i) for i in self.ty.array_element_type]

    @property
    def element_count(self) -> int:
        """
        Gets the count of elements in the array.
        """
        return self.ty.array_num_elements

class QirStructType(QirType):
    """
    Instances of QirStructType represent an anonymous struct with inline defined types in QIR.
    """

    @property
    def struct_element_types(self) -> List[QirType]:
        """
        Gets the ordered list of QirTypes representing the underlying struct types.
        """
        return [QirType(i) for i in self.ty.struct_element_types]

class QirNamedStructType(QirType):
    """
    Instances of QirNamedStruct represent a globally defined struct, often used to represent opaque
    poitners.
    """

    @property
    def name(self) -> str:
        """
        Gets the name of this struct.
        """
        return self.ty.named_struct_name

class QirQubitType(QirNamedStructType):
    """
    Instances of QirQubitType are specific QIR opaque pointer corresponding to the Qubit special
    type.
    """
    pass

class QirResultType(QirNamedStructType):
    """
    Instances of QirResultType are specific QIR opaque pointer corresponding to the Result special
    type.
    """
    pass

class QirOperand:
    """
    Instances of QirOperand represent an instance in a QIR program, either a local operand (variable)
    or constant.
    """
    def __new__(cls, op: PyQirOperand):
        if op.is_local:
            return super().__new__(QirLocalOperand)
        elif op.is_constant:
            if op.constant.is_qubit:
                return super().__new__(QirQubitConstant)
            elif op.constant.is_result:
                return super().__new__(QirResultConstant)
            elif op.constant.is_int:
                return super().__new__(QirIntConstant)
            elif op.constant.is_float:
                return super().__new__(QirDoubleConstant)
            elif op.constant.is_null:
                return super().__new__(QirNullConstant)
            else:
                return super().__new__(cls)
        else:
            return super().__new__(cls)

    def __init__(self, op: PyQirOperand):
        self.op = op
        self.const = op.constant

class QirLocalOperand(QirOperand):
    """
    Instances of QirLocalOperand represent a typed local variable in a QIR program.
    """

    @property
    def name(self) -> str:
        """
        Gets the name identifier for this operand. This could be an identifier from the original
        source language, a generated name based on an identifier, or a generated integer name.
        """
        return self.op.local_name

    @property
    def type(self) -> QirType:
        """
        Gets the QirType instance representing the type for this operand.
        """
        return QirType(self.op.local_type)

class QirConstant(QirOperand):
    """
    Instances of QirConstant represent a constant value in a QIR program.
    """

    @property
    def type(self) -> QirType:
        """
        Gets the QirType instance representing the type of this constant.
        """
        return QirType(self.const.type)

class QirIntConstant(QirConstant):
    """
    Instances of QirIntConstant represent a constant integer value in a QIR program.
    """

    @property
    def value(self) -> int:
        """
        Gets the integer value for this constant.
        """
        return self.const.int_value
    
    @property
    def width(self) -> int:
        """
        Gets the bit width for this integer constant.
        """
        return self.const.int_width

class QirDoubleConstant(QirConstant):
    """
    Instances of QirDoubleConstant represent a constant double-sized float value in a QIR program.
    """

    @property
    def value(self) -> float:
        """
        Gets the double-sized float value for this constant.
        """
        return self.const.float_double_value

class QirNullConstant(QirConstant):
    """
    Instances of QirNullConstant represent a constant null pointer in a QIR program. Use the type
    property to inspect which pointer type this null represents.
    """

    @property
    def value(self):
        """
        The value of QirNullConstant instances is always None.
        """
        return None

class QirQubitConstant(QirConstant):
    """
    Instances of QirQubitConstant represent a statically allocated qubit id in a QIR program.
    """

    @property
    def value(self) -> int:
        """
        Gets the integer identifier for this qubit constant.
        """
        return self.const.qubit_static_id

    @property
    def id(self) -> int:
        """
        Gets the integer identifier for this qubit constant.
        """
        return self.value

class QirResultConstant(QirConstant):
    """
    Instances of QirResultConstant represent a statically allocated result id in a QIR program.
    """

    @property
    def value(self) -> int:
        """
        Gets the integer identifier for the is result constant.
        """
        return self.const.result_static_id

    @property
    def id(self) -> int:
        """
        gets the integer identifier for this result constant.
        """
        return self.value

class QirTerminator:
    """
    Instances of QirTerminator represent the special final instruction at the end of a block that
    indicates how control flow should transfer.
    """
    
    def __new__(cls, term: PyQirTerminator):
        if term.is_ret:
            return super().__new__(QirRetTerminator)
        elif term.is_br:
            return super().__new__(QirBrTerminator)
        elif term.is_condbr:
            return super().__new__(QirCondBrTerminator)
        elif term.is_switch:
            return super().__new__(QirSwitchTerminator)
        elif term.is_unreachable:
            return super().__new__(QirUnreachableTerminator)
        else:
            return super().__new__(cls)

    def __init__(self, term: PyQirTerminator) -> None:
        self.term = term

class QirRetTerminator(QirTerminator):
    """
    Instances of QirRetTerminator represent the ret instruction in a QIR program.
    """

    @property
    def operand(self) -> QirOperand:
        """
        Gets the operand that will be returned by the ret instruction.
        """
        return QirOperand(self.term.ret_operand)

class QirBrTerminator(QirTerminator):
    """
    Instances of QirBrTerminator represent a branch terminator instruction that unconditionally
    jumps execution to the named destination block.
    """

    @property
    def dest(self) -> str:
        """
        Gets the name of the block this branch jumps to.
        """
        return self.term.br_dest

class QirCondBrTerminator(QirTerminator):
    """
    Instances of QirCondBrTerminator represent a conditional branch terminator instruction that
    decides which named block to jump to based on an given operand.
    """

    @property
    def condition(self) -> QirOperand:
        """
        Gets the QirOperand representing the condition used to determine the block to jump to.
        """
        return QirOperand(self.term.condbr_condition)

    @property
    def true_dest(self) -> str:
        """
        Gets the name of the block that will be jumped to if the condition evaluates to true.
        """
        return self.term.condbr_true_dest

    @property
    def false_dest(self) -> str:
        """
        Gets the name of the block that will be jumped to if the condition evaluates to false.
        """
        return self.term.condbr_false_dest

class QirSwitchTerminator(QirTerminator):
    """
    Instances of QirSwitchTerminator represent a switch terminator instruction that can jump
    to one or more blocks based on matching values of a given operand, or jump to a fallback block
    in the case that no matches are found.
    """
    
    @property
    def operand(self) -> QirLocalOperand:
        """
        Gets the operand variable of the switch statement.
        """
        return QirLocalOperand(self.term.switch_operand)

    @property
    def dest_pairs(self) -> List[Tuple[QirConstant, str]]:
        """
        Gets a list of pairs representing the constant values to compare the operand against and the
        matching block name to jump to if the comparison succeeds.
        """
        return [(QirConstant(p[0]), p[1]) for p in self.term.switch_dests]

    @property
    def default_dest(self) -> str:
        """
        Gets the name of the default block that the switch will jump to if no values match the given
        operand.
        """
        return self.term.switch_default_dest

class QirUnreachableTerminator(QirTerminator):
    """
    Instances of QirUnreachableTerminator represent an unreachable terminator instruction. As the name
    implies, this terminator is not expected to be reached such that some instruction in the block
    before this terminator should halt program execution.
    """
    pass

class QirInstr:
    """
    Instances of QirInstr represent an instruction within a block of a QIR program. See the subclasses
    of this type for specifically supported instructions.
    """

    def __new__(cls, instr: PyQirInstruction):
        if instr.is_qis_call:
            return super().__new__(QirQisCallInstr)
        elif instr.is_rt_call:
            return super().__new__(QirRtCallInstr)
        elif instr.is_qir_call:
            return super().__new__(QirQirCallInstr)
        elif instr.is_call:
            return super().__new__(QirCallInstr)
        elif instr.is_add:
            return super().__new__(QirAddInstr)
        elif instr.is_sub:
            return super().__new__(QirSubInstr)
        elif instr.is_mul:
            return super().__new__(QirMulInstr)
        elif instr.is_udiv:
            return super().__new__(QirUDivInstr)
        elif instr.is_sdiv:
            return super().__new__(QirSDivInstr)
        elif instr.is_urem:
            return super().__new__(QirURemInstr)
        elif instr.is_srem:
            return super().__new__(QirSRemInstr)
        elif instr.is_and:
            return super().__new__(QirAndInstr)
        elif instr.is_or:
            return super().__new__(QirOrInstr)
        elif instr.is_xor:
            return super().__new__(QirXorInstr)
        elif instr.is_shl:
            return super().__new__(QirShlInstr)
        elif instr.is_lshr:
            return super().__new__(QirLShrInstr)
        elif instr.is_ashr:
            return super().__new__(QirAShrInstr)
        elif instr.is_fadd:
            return super().__new__(QirFAddInstr)
        elif instr.is_fsub:
            return super().__new__(QirFSubInstr)
        elif instr.is_fmul:
            return super().__new__(QirFMulInstr)
        elif instr.is_fdiv:
            return super().__new__(QirFDivInstr)
        elif instr.is_frem:
            return super().__new__(QirFRemInstr)
        elif instr.is_fneg:
            return super().__new__(QirFNegInstr)
        elif instr.is_icmp:
            return super().__new__(QirICmpInstr)
        elif instr.is_fcmp:
            return super().__new__(QirFCmpInstr)
        elif instr.is_phi:
            return super().__new__(QirPhiInstr)
        else:
            return super().__new__(cls)

    def __init__(self, instr: PyQirInstruction):
        self.instr = instr

    @property
    def output_name(self) -> Optional[str]:
        """
        Gets the name of the local operand that receives the output of this instruction, or
        None if the instruction does not return a value.
        """
        return self.instr.output_name

    @property
    def type(self) -> QirType:
        """
        Gets the QirType instance representing the output of this instruction. If the instruction
        has no output, the type will be an instance of QirVoidType.
        """
        return QirType(self.instr.type)

class QirOpInstr(QirInstr):
    """
    Instances of QirOpInstr represent the class of instructions that have one or more operands that
    they operate on.
    """

    @property
    def target_operands(self) -> List[QirOperand]:
        """
        Gets the list of operands that this instruction operates on.
        """
        return [QirOperand(i) for i in self.instr.target_operands]

class QirAddInstr(QirOpInstr):
    """
    Instances of QirAddIntr represent an integer addition instruction that takes two operands.
    """
    pass

class QirSubInstr(QirOpInstr):
    """
    Instances of QirSubIntr represent an integer subtraction instruction that takes two operands.
    """
    pass

class QirMulInstr(QirOpInstr):
    """
    Instances of QirMulIntr represent an integer multiplication instruction that takes two operands.
    """
    pass

class QirUDivInstr(QirOpInstr):
    """
    Instances of QirUDivIntr represent an unsigned integer division instruction that takes two operands.
    """
    pass

class QirSDivInstr(QirOpInstr):
    """
    Instances of QirSDivIntr represent a signed integer division instruction that takes two operands.
    """
    pass

class QirURemInstr(QirOpInstr):
    """
    Instances of QirURemIntr represent an unsigned integer remainder instruction that takes two operands.
    """
    pass

class QirSRemInstr(QirOpInstr):
    """
    Instances of QirSRemIntr represent a signed integer remainder instruction that takes two operands.
    """
    pass

class QirAndInstr(QirOpInstr):
    """
    Instances of QirAndIntr represent a boolean and instruction that takes two operands.
    """
    pass

class QirOrInstr(QirOpInstr):
    """
    Instances of QirOrIntr represent a boolean or instruction that takes two operands.
    """
    pass

class QirXorInstr(QirOpInstr):
    """
    Instances of QirXorIntr represent a boolean xor instruction that takes two operands.
    """
    pass

class QirShlInstr(QirOpInstr):
    """
    Instances of QirShlIntr represent a bitwise shift left instruction that takes two operands.
    """
    pass

class QirLShrInstr(QirOpInstr):
    """
    Instances of QirLShrIntr represent a logical bitwise shift right instruction that takes two operands.
    """
    pass

class QirAShrInstr(QirOpInstr):
    """
    Instances of QirLShrIntr represent an arithmetic bitwise shift right instruction that takes two operands.
    """
    pass

class QirFAddInstr(QirOpInstr):
    """
    Instances of QirFAddIntr represent a floating-point addition instruction that takes two operands.
    """
    pass

class QirFSubInstr(QirOpInstr):
    """
    Instances of QirFSubIntr represent a floating-point subtraction instruction that takes two operands.
    """
    pass

class QirFMulInstr(QirOpInstr):
    """
    Instances of QirFMulIntr represent a floating-point multiplication instruction that takes two operands.
    """
    pass

class QirFDivInstr(QirOpInstr):
    """
    Instances of QirFDivIntr represent a floating-point division instruction that takes two operands.
    """
    pass

class QirFRemInstr(QirOpInstr):
    """
    Instances of QirFRemIntr represent a floating-point remainder instruction that takes two operands.
    """
    pass

class QirFNegInstr(QirOpInstr):
    """
    Instances of QirFNegIntr represent a floating-point negation instruction that takes one operand.
    """
    pass

class QirICmpInstr(QirOpInstr):
    """
    Instances of QirICmpIntr represent an integer comparison instruction that takes two operands,
    and uses a specific predicate to output the boolean result of the comparison.
    """

    @property
    def predicate(self) -> str:
        """
        Gets a string representing the predicate operation to perform. Possible values are
        "eq", "ne", "ugt", "uge", "ult", "ule", "sgt", "sge", "slt", and "sle".
        """
        return self.instr.icmp_predicate

class QirFCmpInstr(QirOpInstr):
    """
    Instances of QirFCmpInstr represent a floating-point comparison instruction that takes two operands,
    and uses a specific predicate to output the boolean result of the comparison.
    """

    @property
    def predicate(self) -> str:
        """
        Gets a string representing the predicate operation to perform. Possible values are
        "false", "oeq", "ogt", "oge", "olt", "ole", "one", "ord", "uno", "ueq", "ugt", "uge", "ult",
        "ule", "une", and "true"
        """
        return self.instr.fcmp_predicate

class QirPhiInstr(QirInstr):
    """
    Instances of QirPhiInstr represent a phi instruction that selects a value for an operand based
    on the name of the block that transferred execution to the current block.
    """

    @property
    def incoming_values(self) -> List[Tuple[QirOperand, str]]:
        """
        Gets a list of all the incoming value pairs for this phi node, where each pair is the QirOperand
        for the value to use and the string name of the originating block.
        """
        return [(QirOperand(p[0]), p[1]) for p in self.instr.phi_incoming_values]

    def get_incoming_value_for_name(self, name: str) -> Optional[QirOperand]:
        """
        Gets the QirOperand representing the value for a given originating block, or None if that
        name is not found.
        :param name: the block name to search for.
        """
        op = self.instr.get_phi_incoming_value_for_name(name)
        if isinstance(op, PyQirOperand):
            return QirOperand(op)
        else:
            return None

class QirCallInstr(QirInstr):
    """
    Instances of QirCallInstr represent a call instruction in a QIR program.
    """

    @property
    def func_name(self) -> str:
        """
        Gets the name of the function called by this instruction.
        """
        return self.instr.call_func_name

    @property
    def func_args(self) -> List[QirOperand]:
        """
        Gets the list of QirOperand instances that are passed as arguments to the function call.
        """
        return [QirOperand(i) for i in self.instr.call_func_params]

class QirQisCallInstr(QirCallInstr):
    """
    Instances of QirQisCallInstr represent a call instruction where the function name begins with
    "__quantum__qis__" indicating that it is a function from the QIR quantum intrinsic set.
    """
    pass

class QirRtCallInstr(QirCallInstr):
    """
    Instances of QirRtCallInstr represent a call instruction where the function name begins with
    "__quantum__rt__" indicating that it is a function from the QIR runtime.
    """
    pass

class QirQirCallInstr(QirCallInstr):
    """
    Instances of QirQirCallInstr represent a call instruction where the function name begins with
    "__quantum__qir__" indicating that it is a function from the QIR base profile.
    """
    pass

class QirBlock:
    """
    Instances of the QirBlock type represent a basic block within a function body. Each basic block is
    comprised of a list of instructions executed in sequence and a single, special final instruction 
    called a terminator that indicates where execution should jump at the end of the block.
    """

    def __init__(self, block: PyQirBasicBlock):
        self.block = block

    @property
    def name(self) -> str:
        """
        Gets the identifying name for this block. This is unique within a given function and acts
        as a label for any branches that transfer execution to this block.
        """
        return self.block.name

    @property
    def instructions(self) -> List[QirInstr]:
        """
        Gets the list of instructions that make up this block. The list is ordered; instructions are
        executed from first to last unconditionally. This list does not include the special 
        terminator instruction (see QirBlock.terminator).
        """
        return [QirInstr(i) for i in self.block.instructions]

    @property
    def terminator(self) -> QirTerminator:
        """
        Gets the terminator instruction for this block. Every block has exactly one terminator
        and it is the last intruction in the block.
        """
        return QirTerminator(self.block.terminator)

    @property
    def phi_nodes(self) -> List[QirPhiInstr]:
        """
        Gets any phi nodes defined for this block. Phi nodes are a special instruction that defines
        variables based on which block transferred execution to this block. A block may have any number
        of phi nodes, but they are always the first instructions in any given block. A block with no
        phi nodes will return an empty list.
        """
        return [QirPhiInstr(i) for i in self.block.phi_nodes]

    def get_phi_pairs_by_source_name(self, name: str) -> List[Tuple[str, QirOperand]]:
        """
        Gets the variable name, variable value pairs for any phi nodes in this block that correspond
        to the given name. If the name doesn't match a block that can branch to this block or if 
        this block doesn't include any phi nodes, the list will be empty.
        """
        return [(p[0], QirOperand(p[1])) for p in self.block.get_phi_pairs_by_source_name(name)]

    @property
    def is_circuit_like(self) -> bool:
        return all(
            isinstance(instruction, (QirQisCallInstr, QirQirCallInstr))
            for instruction in self.instructions
        )
        # TODO: Check all args are qubit constants.

    def as_openqasm_20(self) -> Optional[str]:
        """
        If this block is circuit-like (that is, consists only of quantum instructions),
        converts it to a representation in OpenQASM 2.0; otherwise, returns `None`.

        Note that the returned representation does not include leading phi nodes, nor trailing terminators.
        """
        return export.export_to(self, export.OpenQasm20Exporter(self.name))

    def as_qiskit_circuit(self) -> Optional["qk.QuantumCircuit"]:
        return export.export_to(self, export.QiskitExporter(self.name))

    def as_qutip_circuit(self) -> Optional["qt.qip.circuit.QubitCircuit"]:
        return export.export_to(self, export.QuTiPExporter())


class QirParameter:
    """
    Instances of the QirParameter type describe a parameter in a function definition or declaration. They
    include a type and a name, where the name is used in the function body as a variable.
    """

    def __init__(self, param: PyQirParameter):
        self.param = param

    @property
    def name(self) -> str:
        """
        Gets the name of this parameter, used as the variable identifier within the body of the
        function.
        """
        return self.param.name

    @property
    def type(self) -> QirType:
        """
        Gets the type of this parameter as represented in the QIR.
        """
        return QirType(self.param.type)

class QirFunction:
    """
    Instances of the QirFunction type represent a single function in the QIR program. They
    are made up of one or more blocks that represent function execution flow.
    """

    def __init__(self, func: PyQirFunction):
        self.func = func

    def __repr__(self) -> str:
        return f"<QIR function {self.name} at {id(self):0x}>"

    @property
    def name(self) -> str:
        """
        Gets the string name for this function.
        """
        return self.func.name

    @property
    def parameters(self) -> List[QirParameter]:
        """
        Gets the list of parameters used when calling this function.
        """
        return [QirParameter(i) for i in self.func.parameters]

    @property
    def return_type(self) -> QirType:
        """
        Gets the return type for this function.
        """
        return QirType(self.func.return_type)

    @property
    def blocks(self) -> List[QirBlock]:
        """
        Gets all the basic blocks for this function.
        """
        return [QirBlock(i) for i in self.func.blocks]

    @property
    def required_qubits(self) -> Optional[int]:
        """
        Gets the number of qubits needed to execute this function based on the
        "RequiredQubits" attribute, or None if that attribute is not present.
        """
        return self.func.required_qubits

    @property
    def required_results(self) -> Optional[int]:
        """
        Gets the number of result bits needed to execute this function based on the
        "RequiredResults" attribute, or None if that attribute is not present.
        """
        return self.func.required_results

    def get_attribute_value(self, name: str) -> Optional[str]:
        """
        Gets the string value of the given attribute key name, or None if that attribute
        is missing or has no defined value.
        :param name: the name of the attribute to look for
        """
        return self.func.get_attribute_value(name)

    def get_block_by_name(self, name: str) -> Optional[QirBlock]:
        """
        Gets the block with the given name, or None if no block with that name is found.
        :param name: the name of the block to look for
        """
        b = self.func.get_block_by_name(name)
        if b is not None:
            return QirBlock(b)
        return None

    def get_instruction_by_output_name(self, name: str) -> Optional[QirInstr]:
        """
        Gets the instruction anywhere in the function where the variable with a given name
        is set. Since LLVM requires any variable is defined by only one instruction, this is
        guaranteed to be no more than one instruction. This will return None if no such instruction
        can be found.
        :param name: the name of the variable to search for
        """
        instr = self.func.get_instruction_by_output_name(name)
        if instr is not None:
            return QirInstr(instr)
        return None

    def control_flow_graph(self) -> "rx.Digraph":
        cfg = rx.PyDiGraph(check_cycle=False, multigraph=True)
        blocks = self.blocks
        block_indices = {
            block.name: cfg.add_node(block)
            for block in blocks
        }

        idx_return = cfg.add_node("Return")
        idx_bottom = None

        for idx_block, block in enumerate(blocks):
            term = block.terminator
            if isinstance(term, QirCondBrTerminator):
                cfg.add_edge(idx_block, block_indices[term.true_dest], True)
                cfg.add_edge(idx_block, block_indices[term.false_dest], False)
            elif isinstance(term, QirBrTerminator):
                cfg.add_edge(idx_block, block_indices[term.dest], ())
            elif isinstance(term, QirRetTerminator):
                cfg.add_edge(idx_block, idx_return, ())
            elif isinstance(term, QirSwitchTerminator):
                print(f"Not yet implemented: {term}")
            elif isinstance(term, QirUnreachableTerminator):
                if idx_bottom is None:
                    idx_bottom = cfg.add_node("⊥")
                cfg.add_edge(idx_block, idx_bottom)
            else:
                print(f"Not yet implemented: {term}")

        return cfg

class QirModule:
    """
    Instances of QirModule parse a QIR program from bitcode into an in-memory
    representation for iterating over the program structure. They contain all the
    functions and global definitions from the program.
    """

    def __init__(self, *args):
        if isinstance(args[0], PyQirModule):
            self.module = args[0]
        elif isinstance(args[0], str):
            self.module = module_from_bitcode(args[0])
        else:
            raise TypeError("Unrecognized argument type. Input must be string path to bitcode or PyQirModule object.")

    @property
    def functions(self) -> List[QirFunction]:
        """
        Gets all the functions defined in this module.
        """
        return [QirFunction(i) for i in self.module.functions]


    def get_func_by_name(self, name: str) -> Optional[QirFunction]:
        """
        Gets the function with the given name, or None if no matching function is found.
        :param name: the name of the function to get
        """
        f = self.module.get_func_by_name(name)
        if isinstance(f, PyQirFunction):
            return QirFunction(f)
        else:
            return None

    def get_funcs_by_attr(self, attr: str) -> List[QirFunction]:
        """
        Gets any functions that have an attribute whose name matches the provided string.
        :param attr: the attribute to use when looking for functions
        """
        return [QirFunction(i) for i in self.module.get_funcs_by_attr(attr)]

    @property
    def entrypoint_funcs(self) -> List[QirFunction]:
        """
        Gets any functions with the "EntryPoint" attribute.
        """
        return [QirFunction(i) for i in self.module.get_entrypoint_funcs()]

    @property
    def interop_funcs(self) -> List[QirFunction]:
        """
        Gets any functions with the "InteropFriendly" attribute.
        """
        return [QirFunction(i) for i in self.module.get_interop_funcs()]
