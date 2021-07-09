# Copyright(c) Microsoft Corporation.
# Licensed under the MIT License.

from .pyqir import *


class QirBuilder:
    """
    The QirBuilder objct describes quantum circuits and emits QIR

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
