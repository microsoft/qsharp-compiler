from pyqir import QirBuilder
import pytest

def test_bell():
    builder = QirBuilder("Bell circuit")
    builder.add_quantum_register("qr", 2)
    builder.add_classical_register("qc", 2)
    builder.h("qr0")
    builder.cx("qr0", "qr1")
    builder.m("qr0", "qc0")
    builder.m("qr1", "qc1")
    builder.build("bell_measure.ll")

def test_bell_no_measure():
    builder = QirBuilder("Bell circuit")
    builder.add_quantum_register("qr", 2)
    builder.h("qr0")
    builder.cx("qr0", "qr1")
    builder.build("bell_no_measure.ll")

def test_all_gates():
    builder = QirBuilder("sample")
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
    builder.build("pytest.ll")
