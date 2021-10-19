from pyqir import *
from pyqir import module_from_bitcode
import pytest

def test_bell(tmpdir):
    builder = QirBuilder("Bell circuit")
    builder.add_quantum_register("qr", 2)
    builder.add_classical_register("qc", 2)
    builder.h("qr0")
    builder.cx("qr0", "qr1")
    builder.m("qr0", "qc0")
    builder.m("qr1", "qc1")

    file = tmpdir.mkdir("sub").join("bell_measure.ll")
    print(f'Writing {file}')
    builder.build(str(file))

def test_bell_no_measure(tmpdir):
    builder = QirBuilder("Bell circuit")
    builder.add_quantum_register("qr", 2)
    builder.h("qr0")
    builder.cx("qr0", "qr1")

    builder.dump_machine()

    file = tmpdir.mkdir("sub").join("bell_no_measure.ll")
    print(f'Writing {file}')
    builder.build(str(file))

def test_bernstein_vazirani(tmpdir):
    builder = QirBuilder("Bernstein-Vazirani")
    builder.add_quantum_register("input", 5)
    builder.add_quantum_register("target", 1)
    builder.add_classical_register("output", 5)

    builder.x("target0")

    builder.h("input0")
    builder.h("input1")
    builder.h("input2")
    builder.h("input3")
    builder.h("input4")

    builder.h("target0")

    builder.cx("input1", "target0")
    builder.cx("input3", "target0")
    builder.cx("input4", "target0")

    builder.h("input0")
    builder.h("input1")
    builder.h("input2")
    builder.h("input3")
    builder.h("input4")

    builder.m("input0", "output0")
    builder.m("input1", "output1")
    builder.m("input2", "output2")
    builder.m("input3", "output3")
    builder.m("input4", "output4")

    file = tmpdir.mkdir("sub").join("bernstein_vazirani.ll")
    print(f'Writing {file}')
    builder.build(str(file))

def test_all_gates(tmpdir):
    builder = QirBuilder("All Gates")
    builder.add_quantum_register("q", 4)
    builder.add_quantum_register("control", 1)
    builder.add_classical_register("c", 4)
    builder.add_classical_register("i", 3)
    builder.add_classical_register("j", 2)
    builder.cx("q0", "control0")
    builder.cz("q1", "control0")
    builder.h("q0")
    builder.reset("q0")
    builder.rx(15.0,"q1")
    builder.ry(16.0,"q2")
    builder.rz(17.0,"q3")
    builder.s("q0")
    builder.s_adj("q1")
    builder.t("q2")
    builder.t_adj("q3")
    builder.x("q0")
    builder.y("q1")
    builder.z("q2")

    builder.m("q0", "c0")
    builder.m("q1", "c1")
    builder.m("q2", "c2")
    builder.m("q3", "c3")

    file = tmpdir.mkdir("sub").join("all_gates.ll")
    print(f'Writing {file}')
    builder.build(str(file))

def test_bernstein_vazirani_ir_string():
    builder = QirBuilder("Bernstein-Vazirani")
    builder.add_quantum_register("input", 5)
    builder.add_quantum_register("target", 1)
    builder.add_classical_register("output", 5)

    builder.x("target0")

    builder.h("input0")
    builder.h("input1")
    builder.h("input2")
    builder.h("input3")
    builder.h("input4")

    builder.h("target0")

    builder.cx("input1", "target0")
    builder.cx("input3", "target0")
    builder.cx("input4", "target0")

    builder.h("input0")
    builder.h("input1")
    builder.h("input2")
    builder.h("input3")
    builder.h("input4")

    builder.m("input0", "output0")
    builder.m("input1", "output1")
    builder.m("input2", "output2")
    builder.m("input3", "output3")
    builder.m("input4", "output4")

    ir = builder.get_ir_string()
    assert ir.startswith("; ModuleID = 'Bernstein-Vazirani'")

def test_parser_pythonic():
    mod = QirModule("tests/teleportchain.baseprofile.bc")
    func_name = "TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__Interop"
    func = mod.get_func_by_name(func_name)
    assert(func.name == func_name)
    func_list = mod.functions
    assert(len(func_list) == 1)
    assert(func_list[0].name == func_name)
    interop_funcs = mod.get_funcs_by_attr("InteropFriendly")
    assert(len(interop_funcs) == 1)
    assert(len(mod.interop_funcs) == 1)
    assert(mod.interop_funcs[0].name == interop_funcs[0].name)
    assert(len(mod.entrypoint_funcs) == 0)
    blocks = func.blocks
    assert(len(blocks) == 9)
    assert(blocks[0].name == "entry")
    term = blocks[0].terminator
    assert(isinstance(term, QirTerminator))
    assert(isinstance(term, QirCondBrTerminator))
    assert(term.true_dest == "then0__1.i.i.i")
    assert(term.false_dest == "continue__1.i.i.i")
    assert(term.condition.name == "0")
    assert(blocks[1].terminator.dest == "continue__1.i.i.i")
    assert(blocks[8].terminator.operand.type.width == 8)
    block = func.get_block_by_name("then0__2.i.i3.i")
    assert(isinstance(block.instructions[0], QirQisCallInstr))
    assert(isinstance(block.instructions[0].func_args[0], QirQubitConstant))
    assert(block.instructions[0].func_args[0].id == 5)
    block = func.get_block_by_name("continue__1.i.i2.i")
    var_name = block.terminator.condition.name
    instr = func.get_instruction_by_output_name(var_name)
    assert(isinstance(instr, QirQirCallInstr))
    assert(instr.output_name == var_name)
    assert(instr.func_name == "__quantum__qir__read_result")
    assert(instr.func_args[0].id == 3)


def test_parser():
    mod = module_from_bitcode("tests/teleportchain.baseprofile.bc")
    func_name = "TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__Interop"
    func = mod.get_func_by_name(func_name)
    assert(func.name == func_name)
    assert(len(func.parameters) == 0)
    assert(func.return_type.is_integer)
    func_list = mod.functions
    assert(len(func_list) == 1)
    assert(func_list[0].name == func_name)
    interop_funcs = mod.get_funcs_by_attr("InteropFriendly")
    assert(len(interop_funcs) == 1)
    assert(interop_funcs[0].name == func_name)
    assert(interop_funcs[0].get_attribute_value("requiredQubits") == "6")
    assert(interop_funcs[0].required_qubits == 6)
    blocks = func.blocks
    assert(len(blocks) == 9)
    assert(blocks[0].name == "entry")
    entry_block = func.get_block_by_name("entry")
    assert(entry_block.name == "entry")
    assert(entry_block.terminator.is_condbr)
    assert(not entry_block.terminator.is_ret)
    assert(entry_block.terminator.condbr_true_dest == "then0__1.i.i.i")
    assert(entry_block.terminator.condbr_false_dest == "continue__1.i.i.i")
    assert(blocks[1].terminator.is_br)
    assert(blocks[1].terminator.br_dest == "continue__1.i.i.i")
    assert(blocks[8].terminator.is_ret)
    assert(len(entry_block.instructions) == 11)
    assert(entry_block.instructions[0].is_call)
    assert(entry_block.instructions[0].call_func_name == "__quantum__qis__h__body")
    assert(entry_block.instructions[0].is_qis_call)
    param_list = entry_block.instructions[0].call_func_params
    assert(len(param_list) == 1)
    assert(param_list[0].is_constant)
    assert(param_list[0].constant.is_qubit)
    assert(param_list[0].constant.qubit_static_id == 0)
    assert(entry_block.instructions[8].is_qis_call)
    assert(entry_block.instructions[8].call_func_name == "__quantum__qis__mz__body")
    assert(entry_block.instructions[8].call_func_params[0].constant.qubit_static_id == 1)
    assert(entry_block.instructions[8].call_func_params[1].constant.result_static_id == 0)
    branch_cond = entry_block.terminator.condbr_condition
    assert(branch_cond.local_name == "0")
    assert(entry_block.instructions[10].is_qir_call)
    assert(entry_block.instructions[10].call_func_name == "__quantum__qir__read_result")
    assert(entry_block.instructions[10].call_func_params[0].constant.result_static_id == 0)
    assert(entry_block.instructions[10].has_output)
    assert(entry_block.instructions[10].output_name == "0")
    source_instr = func.get_instruction_by_output_name(branch_cond.local_name)
    assert(source_instr.call_func_params[0].constant.result_static_id == 0)