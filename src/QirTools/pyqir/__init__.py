from .pyqir import *

class QirBuilder:
    def __init__(self, module: str):
        self.pyqir = PyQIR(module)

    def add_quantum_register(self, name, size):
        self.pyqir.add_quantum_register(name, size)

    def add_classical_register(self, name, size):
        self.pyqir.add_classical_register(name, size)

    def add_inst(self, *args, **kwargs):
        self.pyqir.todo()

    def add_controlled(self, *args, **kwargs):
        self.pyqir.todo()

    def add_rotate(self, *args, **kwargs):
        self.pyqir.todo()

    def add_controlled_rotate(self, *args, **kwargs):
        self.pyqir.todo()
        
    def add_measurement(self, qubit: str, control: str):
        self.pyqir.add_measurement(qubit, control)

    def build(self, file_path: str):
        self.pyqir.write(file_path)
