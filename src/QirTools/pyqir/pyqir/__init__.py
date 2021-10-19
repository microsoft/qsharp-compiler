# Copyright(c) Microsoft Corporation.
# Licensed under the MIT License.

from .pyqir import *


class QirBuilder:
    """
    The QirBuilder object describes quantum circuits and emits QIR

    :param module: name of the QIR module
    :type module: str
    """

    def __init__(self, module: str):
        self.pyqir = PyQIR(module)

    def cx(self, control: str, target: str):
        """
        Applies controlled X operation to the target qubit

        :param control: name of the control qubit
        :type control: str
        :param target: name of the target qubit
        :type target: str
        """
        self.pyqir.cx(control, target)

    def cz(self, control: str, target: str):
        """
        Applies controlled Z operation to the target qubit

        :param control: name of the control qubit
        :type control: str
        :param target: name of the target qubit
        :type target: str
        """
        self.pyqir.cz(control, target)

    def h(self, target: str):
        """
        Applies H operation to the target qubit

        :param target: name of the target qubit
        :type target: str
        """
        self.pyqir.h(target)

    def m(self, qubit: str, target: str):
        """
        Applies measurement operation or the source qubit into the target register

        :param qubit: name of the source qubit
        :type qubit: str
        :param target: name of the target register
        :type target: str
        """
        self.pyqir.m(qubit, target)

    def reset(self, target: str):
        """
        Applies Reset operation to the target qubit

        :param target: name of the target qubit
        :type target: str
        """
        self.pyqir.reset(target)

    def rx(self, theta: float, qubit: str):
        """
        Applies Rx operation to the target qubit

        :param theta: rotation value for target qubit
        :type theta: float
        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.rx(theta, qubit)

    def ry(self, theta: float, qubit: str):
        """
        Applies Ry operation to the target qubit

        :param theta: rotation value for target qubit
        :type theta: float
        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.ry(theta, qubit)

    def rz(self, theta: float, qubit: str):
        """
        Applies Rz operation to the target qubit

        :param theta: rotation value for target qubit
        :type theta: float
        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.rz(theta, qubit)

    def s(self, qubit: str):
        """
        Applies S operation to the target qubit

        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.s(qubit)

    def s_adj(self, qubit: str):
        """
        Applies SAdj operation to the target qubit

        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.s_adj(qubit)

    def t(self, qubit: str):
        """
        Applies T operation to the target qubit

        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.t(qubit)

    def t_adj(self, qubit: str):
        """
        Applies TAdj operation to the target qubit

        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.t_adj(qubit)

    def x(self, qubit: str):
        """
        Applies X operation to the target qubit

        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.x(qubit)

    def y(self, qubit: str):
        """
        Applies Y operation to the target qubit

        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.y(qubit)

    def z(self, qubit: str):
        """
        Applies Z operation to the target qubit

        :param qubit: name of the target qubit
        :type qubit: str
        """
        self.pyqir.z(qubit)

    def dump_machine(self):
        """

        """
        self.pyqir.dump_machine()

    def add_classical_register(self, name: str, size: int):
        """
        Models a classical register of the given size. The individual values
        are accessed by name "<name><index>" with 0 based indicies.
        Example:
            builder = QirBuilder("Bell circuit")
            builder.add_quantum_register("qr", 2)
            builder.add_classical_register("qc", 2)
            builder.h("qr0")
            builder.cx("qr0", "qr1")
            builder.m("qr0", "qc0")
            builder.m("qr1", "qc1")
            builder.build("bell_measure.ll")

        :param name: name of the register
        :type name: str
        :param size: size of the register
        :type size: int
        """
        self.pyqir.add_classical_register(name, size)

    def add_quantum_register(self, name: str, size: int):
        """
        Models an array of qubits of the given size. The individual values
        are accessed by name "<name><index>" with 0 based indicies.
        Example:
            builder = QirBuilder("Bell circuit")
            builder.add_quantum_register("qr", 2)
            builder.add_classical_register("qc", 2)
            builder.h("qr0")
            builder.cx("qr0", "qr1")
            builder.m("qr0", "qc0")
            builder.m("qr1", "qc1")
            builder.build("bell_measure.ll")

        :param name: name of the register
        :type name: str
        :param size: size of the register
        :type size: int
        """
        self.pyqir.add_quantum_register(name, size)

    def build(self, file_path: str):
        """
        Writes the modeled circuit to the supplied file.

        :param file_path: file path of generated QIR
        :type file_path: str
        """
        self.pyqir.write(file_path)

    def get_ir_string(self):
        """
        Returns the modeled circuit as a string.
        """
        return self.pyqir.get_ir_string()

    def enable_logging(self):
        """
        Enables the logging infrastructure
        Controlled via the RUST_LOG environment variable.
        See https://docs.rs/env_logger/0.9.0/env_logger/#enabling-logging for details

        Example:
        in tests.py:
            def test_logging():
                builder = QirBuilder("logging test")
                builder.enable_logging()
                builder.add_quantum_register("qr", 1)
                builder.h("qr0")
                builder.build("test.ll")

        PowerShell:
            $env:RUST_LOG="info"
            python -m pytest
        Bash:
            RUST_LOG=info python -m pytest

        Example Output:
        [2021-09-15T16:55:46Z INFO  pyqir::python] Adding qr[0]
        [2021-09-15T16:55:46Z INFO  pyqir::python] h => qr0
        """
        self.pyqir.enable_logging()

class QirModule:
    """
    The QirModule object parses a QIR program from bitcode into an in-memory
    representation for iterating over the program structure. It contains all the
    functions and global definitions from the program.

    :param bitcode_path: the path to the bitcode file for the QIR program
    :type bitcode_path: string
    """

    def __init__(self, bitcode_path: str):
        self.module = module_from_bitcode(bitcode_path)

    def from_pyqir_module(self, module: PyQirModule):
        self.module = module

    @property
    def functions(self):
        """
        Gets all the functions defined in this module.
        :return: a list of functions in the module
        :rtype: list[QirFunction]
        """
        return list(map(QirFunction, self.module.functions))


    def get_func_by_name(self, name: str):
        """
        Gets the function with the given name, or None if no matching function is found.

        :param name: the name of the function to get
        :type name: string
        :return: the function matchign the name, or None if not found
        :rtype: QirFunction or None
        """
        return QirFunction(self.module.get_func_by_name(name))

    def get_funcs_by_attr(self, attr: str):
        """
        Gets any functions that have an attribute whose name matches the provided string.

        :param attr: the attribute to use when looking for functions
        :type attr: string
        :return: a list of functions
        :rtype: list[QirFunction]
        """
        return list(map(QirFunction, self.module.get_funcs_by_attr(attr)))

    @property
    def entrypoint_funcs(self):
        """
        Gets any functions with the "EntryPoint" attribute.
        :return: a list of functions
        :rtype: list[QirFunction]
        """
        return list(map(QirFunction, self.module.get_entrypoint_funcs()))

    @property
    def interop_funcs(self):
        """
        Gets any functions with the "InteropFriendly" attribute.
        :return: a list of functions
        :rtype: list[QirFunction]
        """
        return list(map(QirFunction, self.module.get_interop_funcs()))

class QirFunction:
    """
    The QirFunction object represents a single function in the QIR program. It
    is made up of one or more blocks that represent function execution flow.

    :param func: the function object from the underlying representation
    :param type: PyQirFunction
    """
    def __init__(self, func: PyQirFunction):
        self.func = func

    @property
    def name(self):
        """
        Gets the string name for this function.
        :return: the string name of the function
        :rtype: str
        """
        return self.func.name

    @property
    def parameters(self):
        """
        Gets the list of parameters used when calling this function.
        :return: the list of parameters
        :rtype: list[QirParameter]
        """
        return list(map(QirParameter, self.func.parameters))

    @property
    def return_type(self):
        """
        Gets the return type for this function.
        :return: the type of the function
        :rtype: QirType
        """
        return QirType(self.func.return_type)

    @property
    def blocks(self):
        """
        Gets all the basic blocks for this function.
        :return: a list of all basic blocks
        :rtype: list[QirBlock]
        """
        return list(map(QirBlock, self.func.blocks))

    @property
    def required_qubits(self):
        """
        Gets the number of qubits needed to execute this function based on the
        "RequiredQubits" attribute, or None if that attribute is not present.
        :return: the number of qubits needed or None
        :rtype: int or None
        """
        return self.func.required_qubits

    @property
    def required_results(self):
        """
        Gets the number of result bits needed to execute this function based on the
        "RequiredResults" attribute, or None if that attribute is not present.
        :return: the number of results needed or None
        :rtype: int or None
        """
        return self.func.required_results

    def get_attribute_value(self, name: str):
        """
        Gets the string value of the given attribute key name, or None if that attribute
        is missing or has no defined value.
        :param name: the name of the attribute to look for
        :type name: str
        :return: the value of the attribute or None
        :rtype: str
        """
        return self.func.get_attribute_value(name)

    def get_block_by_name(self, name: str):
        """
        Gets the block with the given name, or None if no block with that name is found.
        :param name: the name of the block to look for
        :type name: str
        :return: the QirBlock with that name or None
        :rtype: QirBlock
        """
        b = self.func.get_block_by_name(name)
        if b is not None:
            return QirBlock(b)
        return None

    def get_instruction_by_output_name(self, name: str):
        """
        Gets the instruction anywhere in the function where the variable with a given name
        is set. Since LLVM requires any variable is defined by only one instruction, this is
        guaranteed to be no more than one instruction. This will return None if no such instruction
        can be found.
        :param name: the name of the variable to search for
        :type name: str
        :return: the QirInstruction that defines that variable or None
        :rtype: QirInstruction
        """
        instr = self.func.get_instruction_by_output_name(name)
        if instr is not None:
            return QirInstruction(instr)
        return None

class QirParameter:
    """
    The QirParameter object describes a parameter in a function definition or declaration. It
    includes a type and a name, where the name is used in the function body as a variable.
    :param param: the the parameter object from the underlying representation
    :type param: PyQirParameter
    """
    def __init__(self, param: PyQirParameter):
        self.param = param

    @property
    def name(self):
        """
        Gets the name of this parameter, used as the variable identifier within the body of the
        function.
        :return: the name of the parameter
        :rtype: str
        """
        return self.param.name

    @property
    def type(self):
        """
        Gets the type of this parameter as represented in the QIR.
        :return: the QIR type for this parameter
        :rtype: QirType
        """
        return QirType(self.param.type)

class QirBlock:
    """
    The QirBlock object represents a basic block within a function body. Each basic block is
    comprised of a list of instructions executed in sequence and single, special final instruction 
    called a terminator that indicates where execution should jump at the end of the block.
    :param block: the basic block object from the underlying representation
    :type block: PyQirBasicBlock
    """
    def __init__(self, block: PyQirBasicBlock):
        self.block = block

    @property
    def name(self):
        """
        Gets the identifying name for this block. This is unique within a given function and acts
        as a label for any branches that transfer execution to this block.
        :return: the name for this block
        :rtype: str
        """
        return self.block.name

    @property
    def instructions(self):
        """
        Gets the list of instructions that make up this block. The list is ordered; instructions are
        executed from first to last unconditionally. This list does not include the special 
        terminator instruction (see QirBlock.terminator).
        :return: the list of instructions for the block
        :rtype: list[QirInstruction]
        """
        return list(map(QirInstruction, self.block.instructions))

    @property
    def terminator(self):
        """
        Gets the terminator instruction for this block. Every block has exactly one terminator
        and it is the last intruction in the block.
        :return: the terminator for this block
        :rtype: QirTerminator
        """
        return QirTerminator(self.block.terminator)

    @property
    def phi_nodes(self):
        """
        Gets any phi nodes defined for this block. Phi nodes are a special instruction that defines
        variables based on which block transfered execution to this block. A block may have any number
        of phi nodes, but they are always the first instructions in any given block. A block with no
        phi nodes will return an empty list.
        :return: the phi nodes, if any, for this block.
        :rtype: list[QirInstruction]
        """
        return list(map(QirInstruction, self.block.phi_nodes))

    def get_phi_pairs_by_source_name(self, name: str):
        """
        Gets the variable name, variable value pairs for any phi nodes in this block that correspond
        to the given name. If the name doesn't match a block that can branch to this block or if 
        this block doesn't include any phi nodes, the list will be empty.
        :return: the list of name-value pairs for the given source block name
        :rtype: list[(str, QirOperand)]
        """
        return list(map(lambda p: (p[0], QirOperand(p[1])) ,self.get_phi_pairs_by_source_name(name)))

class QirInstruction:
    def __new__(cls, instr: PyQirInstruction):
        if instr.is_qis_call:
            return super().__new__(QirQisCallInstr)
        elif instr.is_qrt_call:
            return super().__new__(QirQrtCallInstr)
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
        else:
            return super().__new__(cls)

    def __init__(self, instr: PyQirInstruction):
        self.instr = instr

    @property
    def output_name(self):
        return self.instr.output_name

    @property
    def type(self):
        return QirType(self.instr.type)

class QirOpInstr(QirInstruction):
    @property
    def target_operands(self):
        return list(map(QirOperand, self.instr.target_operands))

class QirAddInstr(QirOpInstr):
    pass

class QirSubInstr(QirOpInstr):
    pass

class QirMulInstr(QirOpInstr):
    pass

class QirUDivInstr(QirOpInstr):
    pass

class QirSDivInstr(QirOpInstr):
    pass

class QirURemInstr(QirOpInstr):
    pass

class QirSRemInstr(QirOpInstr):
    pass

class QirAndInstr(QirOpInstr):
    pass

class QirOrInstr(QirOpInstr):
    pass

class QirXorInstr(QirOpInstr):
    pass

class QirShlInstr(QirOpInstr):
    pass

class QirLShrInstr(QirOpInstr):
    pass

class QirAShrInstr(QirOpInstr):
    pass

class QirFAddInstr(QirOpInstr):
    pass

class QirFSubInstr(QirOpInstr):
    pass

class QirFMulInstr(QirOpInstr):
    pass

class QirFDivInstr(QirOpInstr):
    pass

class QirFRemInstr(QirOpInstr):
    pass

class QirFNegInstr(QirOpInstr):
    pass

class QirICmpInstr(QirOpInstr):
    @property
    def predicate(self):
        return self.instr.icmp_predicate

class QirFCmpInstr(QirOpInstr):
    @property
    def predicate(self):
        return self.instr.fcmp_predicate

class QirPhiInstr(QirInstruction):
    @property
    def incoming_values(self):
        return list(map(lambda p: (QirOperand(p[0]), p[1]), self.instr.phi_incoming_values))

    def get_incoming_values_for_name(self, name: str):
        return list(map(QirOperand, self.instr.get_phi_incoming_values_for_name(name)))

class QirCallInstr(QirInstruction):
    @property
    def func_name(self):
        return self.instr.call_func_name

    @property
    def func_args(self):
        return list(map(QirOperand, self.instr.call_func_params))

class QirQisCallInstr(QirCallInstr):
    pass

class QirQrtCallInstr(QirCallInstr):
    pass

class QirQirCallInstr(QirCallInstr):
    pass

class QirTerminator:
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
    @property
    def operand(self):
        return QirOperand(self.term.ret_operand)

class QirBrTerminator(QirTerminator):
    @property
    def dest(self):
        return self.term.br_dest

class QirCondBrTerminator(QirTerminator):
    @property
    def condition(self):
        return QirOperand(self.term.condbr_condition)

    @property
    def true_dest(self):
        return self.term.condbr_true_dest

    @property
    def false_dest(self):
        return self.term.condbr_false_dest

class QirSwitchTerminator(QirTerminator):
    pass

class QirUnreachableTerminator(QirTerminator):
    pass

class QirOperand:
    def __new__(cls, op: PyQirOperand):
        if op.is_local:
            return super().__new__(QirLocalOperand)
        elif op.is_constant:
            return QirConstant(op.constant)
        else:
            return super().__new__(cls)

    def __init__(self, op: PyQirOperand):
        self.op = op

class QirLocalOperand(QirOperand):
    @property
    def name(self):
        return self.op.local_name

    @property
    def type(self):
        return QirType(self.op.local_type)

class QirConstant:
    def __new__(cls, const: PyQirConstant):
        if const.is_qubit:
            return super().__new__(QirQubitConstant)
        elif const.is_result:
            return super().__new__(QirResultConstant)
        elif const.is_int:
            return super().__new__(QirIntConstant)
        elif const.is_float:
            return super().__new__(QirDoubleConstant)
        elif const.is_null:
            return super().__new__(QirNullConstant)
        else:
            return super().__new__(cls)

    def __init__(self, const: PyQirConstant):
        self.const = const

    @property
    def type(self):
        return QirType(self.const.type)

class QirIntConstant(QirConstant):
    @property
    def value(self):
        return self.const.int_value
    
    @property
    def width(self):
        return self.const.int_width

class QirDoubleConstant(QirConstant):
    @property
    def value(self):
        return self.const.float_double_value

class QirNullConstant(QirConstant):
    pass

class QirQubitConstant(QirConstant):
    @property
    def id(self):
        return self.const.qubit_static_id

class QirResultConstant(QirConstant):
    @property
    def id(self):
        return self.const.result_static_id

class QirType:
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
            return super().__new__(QirNamedStruct)
        else:
            return super().__new__(cls)

    def __init__(self, ty: PyQirType):
        self.ty = ty

class QirVoidType(QirType):
    pass

class QirIntegerType(QirType):
    @property
    def width(self):
        return self.ty.integer_width

class QirPointerType(QirType):
    @property
    def type(self):
        return QirType(self.ty.pointer_type)

    @property
    def addrspace(self):
        return self.ty.pointer_addrspace

class QirDoubleType(QirType):
    pass

class QirArrayType(QirType):
    @property
    def element_types(self):
        return list(map(QirType, self.ty.array_element_type))

    @property
    def element_count(self):
        return self.ty.array_num_elements

class QirStructType(QirType):
    @property
    def struct_element_types(self):
        return list(map(QirType, self.ty.struct_element_types))

class QirNamedStruct(QirType):
    @property
    def name(self):
        return self.ty.named_struct_name

class QirQubitType(QirPointerType):
    pass

class QirResultType(QirPointerType):
    pass
