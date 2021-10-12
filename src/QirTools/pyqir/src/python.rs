// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use log;

use pyo3::PyErr;
use pyo3::{exceptions::PyOSError, prelude::*};
use qirlib::interop::{
    ClassicalRegister, Controlled, Instruction, Measured, QuantumRegister, Rotated, SemanticModel,
    Single,
};

#[pymodule]
fn pyqir(_py: Python<'_>, m: &PyModule) -> PyResult<()> {
    m.add_class::<PyQIR>()?;

    Ok(())
}

#[pyclass]
pub struct PyQIR {
    pub(super) model: SemanticModel,
}

#[pymethods]
impl PyQIR {
    #[new]
    fn new(name: String) -> Self {
        PyQIR {
            model: SemanticModel::new(name),
        }
    }

    fn add_measurement(&mut self, qubit: String, target: String) -> PyResult<()> {
        log::info!("measure {} => {}", qubit, target);
        Ok(())
    }

    fn cx(&mut self, control: String, target: String) -> PyResult<()> {
        log::info!("cx {} => {}", control, target);
        let controlled = Controlled::new(control, target);
        let inst = Instruction::Cx(controlled);
        self.model.add_inst(inst);
        Ok(())
    }

    fn cz(&mut self, control: String, target: String) -> PyResult<()> {
        log::info!("cz {} => {}", control, target);
        let controlled = Controlled::new(control, target);
        let inst = Instruction::Cz(controlled);
        self.model.add_inst(inst);
        Ok(())
    }

    fn h(&mut self, qubit: String) -> PyResult<()> {
        log::info!("h => {}", qubit);
        let single = Single::new(qubit);
        let inst = Instruction::H(single);
        self.model.add_inst(inst);
        Ok(())
    }

    fn m(&mut self, qubit: String, target: String) -> PyResult<()> {
        log::info!("m {}[{}]", qubit, target);
        let inst = Measured::new(qubit, target);
        let inst = Instruction::M(inst);
        self.model.add_inst(inst);
        Ok(())
    }

    fn reset(&mut self, qubit: String) -> PyResult<()> {
        log::info!("reset => {}", qubit);
        let single = Single::new(qubit);
        let inst = Instruction::Reset(single);
        self.model.add_inst(inst);
        Ok(())
    }

    fn rx(&mut self, theta: f64, qubit: String) -> PyResult<()> {
        log::info!("rx {} => {}", qubit, theta);
        let rotated = Rotated::new(theta, qubit);
        let inst = Instruction::Rx(rotated);
        self.model.add_inst(inst);
        Ok(())
    }

    fn ry(&mut self, theta: f64, qubit: String) -> PyResult<()> {
        log::info!("ry {} => {}", qubit, theta);
        let rotated = Rotated::new(theta, qubit);
        let inst = Instruction::Ry(rotated);
        self.model.add_inst(inst);
        Ok(())
    }

    fn rz(&mut self, theta: f64, qubit: String) -> PyResult<()> {
        log::info!("rz {} => {}", qubit, theta);
        let rotated = Rotated::new(theta, qubit);
        let inst = Instruction::Rz(rotated);
        self.model.add_inst(inst);
        Ok(())
    }

    fn s(&mut self, qubit: String) -> PyResult<()> {
        log::info!("s => {}", qubit);
        let single = Single::new(qubit);
        let inst = Instruction::S(single);
        self.model.add_inst(inst);
        Ok(())
    }

    fn s_adj(&mut self, qubit: String) -> PyResult<()> {
        log::info!("s_adj => {}", qubit);
        let single = Single::new(qubit);
        let inst = Instruction::SAdj(single);
        self.model.add_inst(inst);
        Ok(())
    }

    fn t(&mut self, qubit: String) -> PyResult<()> {
        log::info!("t => {}", qubit);
        let single = Single::new(qubit);
        let inst = Instruction::T(single);
        self.model.add_inst(inst);
        Ok(())
    }

    fn t_adj(&mut self, qubit: String) -> PyResult<()> {
        log::info!("t_adj => {}", qubit);
        let single = Single::new(qubit);
        let inst = Instruction::TAdj(single);
        self.model.add_inst(inst);
        Ok(())
    }

    fn x(&mut self, qubit: String) -> PyResult<()> {
        log::info!("x => {}", qubit);
        let single = Single::new(qubit);
        let inst = Instruction::X(single);
        self.model.add_inst(inst);
        Ok(())
    }

    fn y(&mut self, qubit: String) -> PyResult<()> {
        log::info!("y => {}", qubit);
        let single = Single::new(qubit);
        let inst = Instruction::Y(single);
        self.model.add_inst(inst);
        Ok(())
    }

    fn dump_machine(&mut self) -> PyResult<()> {
        log::info!("dump_machine");
        let inst = Instruction::DumpMachine;
        self.model.add_inst(inst);
        Ok(())
    }

    fn z(&mut self, qubit: String) -> PyResult<()> {
        log::info!("z => {}", qubit);
        let single = Single::new(qubit);
        let inst = Instruction::Z(single);
        self.model.add_inst(inst);
        Ok(())
    }

    fn add_quantum_register(&mut self, name: String, size: u64) -> PyResult<()> {
        let ns = name.as_str();
        for index in 0..size {
            let register_name = format!("{}[{}]", ns, index);
            log::info!("Adding {}", register_name);
            let reg = QuantumRegister {
                name: String::from(ns),
                index,
            };
            self.model.add_reg(reg.as_register());
        }
        Ok(())
    }

    fn add_classical_register(&mut self, name: String, size: u64) -> PyResult<()> {
        let ns = name.clone();
        let reg = ClassicalRegister { name, size };
        log::info!("Adding {}({})", ns, size);
        self.model.add_reg(reg.as_register());
        Ok(())
    }

    fn write(&self, file_name: &str) -> PyResult<()> {
        if let Err(msg) = qirlib::emit::write(&self.model, file_name) {
            let err: PyErr = PyOSError::new_err::<String>(msg);
            return Err(err);
        }
        Ok(())
    }

    fn get_ir_string(&self) -> PyResult<String> {
        match qirlib::emit::get_ir_string(&self.model) {
            Err(msg) => {
                let err: PyErr = PyOSError::new_err::<String>(msg);
                Err(err)
            }
            Ok(ir) => Ok(ir),
        }
    }

    fn enable_logging(&self) -> PyResult<()> {
        let _ = env_logger::try_init();
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use serial_test::serial;
    use tempfile::tempdir;

    fn init() {
        let _ = env_logger::builder()
            .filter_level(log::LevelFilter::Info)
            .is_test(true)
            .try_init();
    }

    #[test]
    #[serial]
    fn noop_e2e_smoke() -> PyResult<()> {
        init();
        let dir = tempdir()?;
        let name = "noop_e2e_smoke";
        let pyqir = PyQIR::new(String::from(name));
        let file_path = dir.path().join(format!("{}.ll", name));

        log::info!("Writing {:?}", file_path);
        pyqir.write(file_path.display().to_string().as_str())?;

        Ok(())
    }

    #[test]
    #[serial]
    fn bell_measure() -> PyResult<()> {
        init();
        let dir = tempdir()?;
        let name = "bell_measure";
        let mut pyqir = PyQIR::new(String::from(name));
        let file_path = dir.path().join(format!("{}.ll", name));

        pyqir.add_quantum_register(String::from("qr"), 2)?;
        pyqir.add_classical_register(String::from("qc"), 2)?;
        pyqir.h(String::from("qr0"))?;
        pyqir.cx(String::from("qr0"), String::from("qr1"))?;
        pyqir.add_measurement(String::from("qr0"), String::from("qc0"))?;
        pyqir.add_measurement(String::from("qr1"), String::from("qc1"))?;

        log::info!("Writing {:?}", file_path);
        pyqir.write(file_path.display().to_string().as_str())?;

        Ok(())
    }

    #[test]
    #[serial]
    fn bell_no_measure() -> PyResult<()> {
        init();
        let dir = tempdir()?;
        let name = "bell_no_measure";
        let mut pyqir = PyQIR::new(String::from(name));
        let file_path = dir.path().join(format!("{}.ll", name));

        pyqir.add_quantum_register(String::from("qr"), 2)?;
        pyqir.add_classical_register(String::from("qc"), 2)?;
        pyqir.h(String::from("qr0"))?;
        pyqir.cx(String::from("qr0"), String::from("qr1"))?;

        log::info!("Writing {:?}", file_path);
        pyqir.write(file_path.display().to_string().as_str())?;
        Ok(())
    }
}
