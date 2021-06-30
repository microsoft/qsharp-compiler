from .pyqir import *

class QirBuilder:
    def __init__(self, module: str):
        self.pyqir = PyQIR(module)

    def cx(self, control:str, target:str):
        self.pyqir.cx(control, target)

    def cz(self, control:str, target:str):
        self.pyqir.cz(control, target)

    def h(self, target: str):
        self.pyqir.h(target)

    def m(self, qubit: str, target: str):
        self.pyqir.m(qubit, target)

    def reset(self, target: str):
        self.pyqir.reset(target)

    def rx(self, theta: float, qubit: str):
        self.pyqir.rx(theta, qubit)

    def ry(self, theta: float, qubit: str):
        self.pyqir.ry(theta, qubit)

    def rz(self, theta: float, qubit: str):
        self.pyqir.rz(theta, qubit)

    def s(self, qubit: str):
        self.pyqir.s(qubit)

    def s_adj(self, qubit: str):
        self.pyqir.s_adj(qubit)

    def t(self, qubit: str):
        self.pyqir.t(qubit)

    def t_adj(self, qubit: str):
        self.pyqir.t_adj(qubit)

    def x(self, qubit: str):
        self.pyqir.x(qubit)

    def y(self, qubit: str):
        self.pyqir.y(qubit)

    def z(self, qubit: str):
        self.pyqir.z(qubit)

    def add_classical_register(self, name, size):
        self.pyqir.add_classical_register(name, size)

    def add_quantum_register(self, name, size):
        self.pyqir.add_quantum_register(name, size)

    def build(self, file_path: str):
        self.pyqir.write(file_path)
