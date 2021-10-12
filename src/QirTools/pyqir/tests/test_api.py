from pyqir import QirBuilder
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
