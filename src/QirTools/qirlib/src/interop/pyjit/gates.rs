#![allow(non_upper_case_globals)]
#![allow(non_camel_case_types)]
#![allow(non_snake_case)]
#![allow(dead_code)]

use lazy_static::lazy_static;
use microsoft_quantum_qir_runtime_sys::QUBIT;
use mut_static::MutStatic;

use crate::interop::{
    ClassicalRegister, Controlled, Instruction, Measured, QuantumRegister, Rotated, SemanticModel,
    Single,
};

lazy_static! {
    pub static ref CURRENT_GATES: MutStatic<BaseProfile> = {
        MutStatic::from(BaseProfile::new())
    };
}

#[derive(Default)]
pub struct BaseProfile {
    model: SemanticModel,
    max_id: QUBIT,
}

pub struct GateScope {}

impl GateScope {
    pub fn new() -> GateScope {
        let mut gs = CURRENT_GATES.write().unwrap();
        gs.reset();
        GateScope {}
    }
}

impl Drop for GateScope {
    fn drop(&mut self) {
        let mut gs = CURRENT_GATES.write().unwrap();
        gs.reset();
    }
}

impl BaseProfile {
    pub fn new() -> Self {
        BaseProfile {
            model: SemanticModel::new(String::from("QIR")),
            max_id: 0,
        }
    }

    pub fn reset(&mut self) {
        self.model = SemanticModel::new(String::from("QIR"));
        self.max_id = 0;
    }

    fn record_max_qubit_id(&mut self, qubit: QUBIT) {
        if qubit > self.max_id {
            self.max_id = qubit
        }
    }
    pub fn get_model(&self) -> SemanticModel {
        self.model.clone()
    }
    pub fn infer_allocations(&mut self) {
        if self.max_id == 0 {
            return;
        }
        for index in 0..self.max_id {
            let qr = QuantumRegister::new(String::from("qubit"), index);
            self.model.add_reg(qr.as_register());
        }
        let cr = ClassicalRegister::new(String::from("output"), self.max_id);
        self.model.add_reg(cr.as_register());
    }

    pub fn cx(&mut self, control: QUBIT, target: QUBIT) {
        self.record_max_qubit_id(control);
        self.record_max_qubit_id(target);

        log::debug!("cx {}:{}", control, target);
        self.model
            .add_inst(Instruction::Cx(BaseProfile::controlled(control, target)));
    }

    pub fn cz(&mut self, control: QUBIT, target: QUBIT) {
        self.record_max_qubit_id(control);
        self.record_max_qubit_id(target);

        log::debug!("cz {}:{}", control, target);
        self.model
            .add_inst(Instruction::Cz(BaseProfile::controlled(control, target)));
    }

    pub fn h(&mut self, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("h {}", qubit);
        self.model
            .add_inst(Instruction::H(BaseProfile::single(qubit)));
    }

    pub fn m(&mut self, qubit: QUBIT, target: QUBIT) {
        self.record_max_qubit_id(qubit);
        self.record_max_qubit_id(target);

        log::debug!("m {}:{}", qubit, target);
        self.model
            .add_inst(Instruction::M(BaseProfile::measured(qubit, target)));
    }

    pub fn rx(&mut self, theta: f64, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("rx {}({})", qubit, theta);
        self.model
            .add_inst(Instruction::Rx(BaseProfile::rotated(theta, qubit)));
    }
    pub fn ry(&mut self, theta: f64, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("ry {}({})", qubit, theta);
        self.model
            .add_inst(Instruction::Ry(BaseProfile::rotated(theta, qubit)));
    }
    pub fn rz(&mut self, theta: f64, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("rz {}({})", qubit, theta);
        self.model
            .add_inst(Instruction::Rz(BaseProfile::rotated(theta, qubit)));
    }
    pub fn s(&mut self, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("s {}", qubit);
        self.model
            .add_inst(Instruction::S(BaseProfile::single(qubit)));
    }
    pub fn s_adj(&mut self, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("s_adj {}", qubit);
        self.model
            .add_inst(Instruction::SAdj(BaseProfile::single(qubit)));
    }

    pub fn t(&mut self, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("t {}", qubit);
        self.model
            .add_inst(Instruction::T(BaseProfile::single(qubit)));
    }
    pub fn t_adj(&mut self, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("t_adj {}", qubit);
        self.model
            .add_inst(Instruction::TAdj(BaseProfile::single(qubit)));
    }

    pub fn x(&mut self, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("x {}", qubit);
        self.model
            .add_inst(Instruction::X(BaseProfile::single(qubit)));
    }
    pub fn y(&mut self, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("y {}", qubit);
        self.model
            .add_inst(Instruction::Y(BaseProfile::single(qubit)));
    }
    pub fn z(&mut self, qubit: QUBIT) {
        self.record_max_qubit_id(qubit);

        log::debug!("z {}", qubit);
        self.model
            .add_inst(Instruction::Z(BaseProfile::single(qubit)));
    }

    pub fn dump_machine(&mut self) {
        log::debug!("dumpmachine");
    }

    fn controlled(control: QUBIT, target: QUBIT) -> Controlled {
        Controlled::new(
            BaseProfile::get_cubit_string(control),
            BaseProfile::get_cubit_string(target),
        )
    }

    fn measured(qubit: QUBIT, target: QUBIT) -> Measured {
        Measured::new(
            BaseProfile::get_cubit_string(qubit),
            BaseProfile::get_cubit_string(target),
        )
    }

    fn rotated(theta: f64, qubit: QUBIT) -> Rotated {
        Rotated::new(theta, BaseProfile::get_cubit_string(qubit))
    }

    fn single(qubit: QUBIT) -> Single {
        Single::new(BaseProfile::get_cubit_string(qubit))
    }

    fn get_cubit_string(qubit: QUBIT) -> String {
        String::from(format!("{}", qubit))
    }
}
