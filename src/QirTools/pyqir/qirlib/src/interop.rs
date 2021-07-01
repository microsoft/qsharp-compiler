#[derive(Clone, Debug, PartialEq, Eq)]
pub enum Register {
    Quantum { name: String, index: u64 },
    Classical { name: String, size: u64 },
}

// https://github.com/microsoft/qsharp-language/blob/ageller/profile/Specifications/QIR/Base-Profile.md
#[derive(Clone, Debug, PartialEq)]
pub enum Instruction {
    Cx { control: String, target: String },
    Cz { control: String, target: String },
    H(String),
    M { qubit: String, target: String },
    Reset(String),
    Rx { theta: f64, qubit: String },
    Ry { theta: f64, qubit: String },
    Rz { theta: f64, qubit: String },
    S(String),
    Sdg(String /*todo!*/),
    T(String),
    Tdg(String /*todo!*/),
    X(String),
    Y(String),
    Z(String),
}

pub struct SemanticModel {
    pub name: String,
    pub registers: Vec<Register>,
    pub qubits: Vec<Register>,
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
            Register::Classical{ name, size } => self.registers.push(reg),
            Register::Quantum {name, index} => self.qubits.push(reg),
        }
    }

    pub fn add_inst(&mut self, inst: Instruction) {
        self.instructions.push(inst);
    }
}
