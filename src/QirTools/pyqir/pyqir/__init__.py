# Copyright(c) Microsoft Corporation.
# Licensed under the MIT License.

from .parser import *
from .builder import *
from typing import Any
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

    def build_with(self, pyobj: Any):
        """
        JIT compiles the circuit delegating quantum operations to the supplied object

        :param pyobj: python GateSet object defining the quantum operations
        :type pyobj: str
        """
        self.pyqir.build_with_python(pyobj)

    def get_ir_string(self):
        """
        Returns the modeled circuit as an LLVM IR module (human readable) string.
        """
        return self.pyqir.get_ir_string()

    def get_bitcode_base64_string(self):
        """
        Returns the modeled circuit as a base64 encoded LLVM bitcode module.
        """
        return self.pyqir.get_bitcode_base64_string()

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


class QirEvaluator(object):
    """
    The QirEvaluator object loads bitcode/QIR for evaluation and processing

    """

    def __init__(self):
        self.pyqir = PyQIR("module")

    def eval(self, file_path: str, pyobj: Any):
        """
        JIT compiles the circuit delegating quantum operations to the supplied object

        :param file_path: file path of existing QIR in a ll or bc file
        :type file_path: str

        :param pyobj: python GateSet object defining the quantum operations
        :type pyobj: str
        """
        self.pyqir.eval(file_path, pyobj)

class GateSet(object):
    def __init__(self):
        self.number_of_qubits = 0

    def cx(self, control: str, target: str):
        pass

    def cz(self, control: str, target: str):
        pass

    def h(self, target: str):
        pass

    def m(self, qubit: str, target: str):
        pass

    def reset(self, target: str):
        pass

    def rx(self, theta: float, qubit: str):
        pass

    def ry(self, theta: float, qubit: str):
        pass

    def rz(self, theta: float, qubit: str):
        pass

    def s(self, qubit: str):
        pass

    def s_adj(self, qubit: str):
        pass

    def t(self, qubit: str):
        pass

    def t_adj(self, qubit: str):
        pass

    def x(self, qubit: str):
        pass

    def y(self, qubit: str):
        pass

    def z(self, qubit: str):
        pass

    def dump_machine(self):
        pass

    def finish(self, metadata: dict):
        self.number_of_qubits = metadata["number_of_qubits"]
        pass
