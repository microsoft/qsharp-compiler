// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct QuantumRegister {
    pub name: String,
    pub index: u64,
}

impl QuantumRegister {
    pub fn new(name: String, index: u64) -> Self {
        QuantumRegister { name, index }
    }

    pub fn as_register(&self) -> Register {
        Register::Quantum(self.clone())
    }
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct ClassicalRegister {
    pub name: String,
    pub size: u64,
}

impl ClassicalRegister {
    pub fn new(name: String, size: u64) -> Self {
        ClassicalRegister { name, size }
    }

    pub fn as_register(&self) -> Register {
        Register::Classical(self.clone())
    }
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub enum Register {
    Quantum(QuantumRegister),
    Classical(ClassicalRegister),
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Controlled {
    pub control: String,
    pub target: String,
}

impl Controlled {
    pub fn new(control: String, target: String) -> Self {
        Controlled { control, target }
    }
}

#[derive(Clone, Debug, PartialEq)]
pub struct Rotated {
    pub theta: f64,
    pub qubit: String,
}

impl Rotated {
    pub fn new(theta: f64, qubit: String) -> Self {
        Rotated { theta, qubit }
    }
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Single {
    pub qubit: String,
}

impl Single {
    pub fn new(qubit: String) -> Self {
        Single { qubit }
    }
}

// https://github.com/microsoft/qsharp-language/blob/ageller/profile/Specifications/QIR/Base-Profile.md
#[derive(Clone, Debug, PartialEq)]
pub enum Instruction {
    Cx(Controlled),
    Cz(Controlled),
    H(Single),
    M { qubit: String, target: String },
    Reset(Single),
    Rx(Rotated),
    Ry(Rotated),
    Rz(Rotated),
    S(Single),
    SAdj(Single),
    T(Single),
    TAdj(Single),
    X(Single),
    Y(Single),
    Z(Single),
}

pub struct SemanticModel {
    pub name: String,
    pub registers: Vec<ClassicalRegister>,
    pub qubits: Vec<QuantumRegister>,
    pub instructions: Vec<Instruction>,
}

impl SemanticModel {
    pub fn new(name: String) -> Self {
        SemanticModel {
            name: name,
            registers: vec![],
            qubits: vec![],
            instructions: vec![],
        }
    }

    pub fn add_reg(&mut self, reg: Register) {
        match &reg {
            Register::Classical(creg) => self.registers.push(creg.to_owned()),
            Register::Quantum(qreg) => self.qubits.push(qreg.to_owned()),
        }
    }

    pub fn add_inst(&mut self, inst: Instruction) {
        self.instructions.push(inst);
    }
}
