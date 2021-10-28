// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use log;
use pyo3::exceptions::{PyOSError, PyRuntimeError};
use pyo3::prelude::*;
use pyo3::types::PyDict;
use pyo3::PyErr;
use qirlib::interop::{
    ClassicalRegister, Controlled, Instruction, Measured, QuantumRegister, Rotated, SemanticModel,
    Single,
};

use crate::parser::*;

#[pymodule]
fn pyqir(_py: Python<'_>, m: &PyModule) -> PyResult<()> {
    m.add_class::<PyQIR>()?;
    m.add_class::<PyQirModule>()?;
    m.add_class::<PyQirFunction>()?;
    m.add_class::<PyQirParameter>()?;
    m.add_class::<PyQirBasicBlock>()?;
    m.add_class::<PyQirInstruction>()?;
    m.add_class::<PyQirTerminator>()?;
    m.add_class::<PyQirOperand>()?;
    m.add_class::<PyQirConstant>()?;
    m.add_class::<PyQirType>()?;

    #[pyfn(m)]
    #[pyo3(name = "module_from_bitcode")]
    fn module_from_bitcode_py(_py: Python, bc_path: String) -> PyResult<PyQirModule> {
        match llvm_ir::Module::from_bc_path(&bc_path) {
            Ok(m) => Ok(PyQirModule { module: m }),
            Err(s) => Err(PyRuntimeError::new_err(s)),
        }
    }

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
        if let Err(msg) = qirlib::interop::emit::write(&self.model, file_name) {
            let err: PyErr = PyOSError::new_err::<String>(msg);
            return Err(err);
        }
        Ok(())
    }

    fn controlled(
        &self,
        pyobj: &PyAny,
        gate: &str,
        control: String,
        target: String,
    ) -> PyResult<()> {
        let has_gate = pyobj.hasattr(gate)?;
        if has_gate {
            let func = pyobj.getattr(gate)?;
            let args = (control, target);
            func.call1(args)?;
        }
        Ok(())
    }

    fn measured(&self, pyobj: &PyAny, gate: &str, qubit: String, target: String) -> PyResult<()> {
        let has_gate = pyobj.hasattr(gate)?;
        if has_gate {
            let func = pyobj.getattr(gate)?;
            let args = (qubit, target);
            func.call1(args)?;
        }
        Ok(())
    }

    fn single(&self, pyobj: &PyAny, gate: &str, qubit: String) -> PyResult<()> {
        let has_gate = pyobj.hasattr(gate)?;
        if has_gate {
            let func = pyobj.getattr(gate)?;
            let args = (qubit,);
            func.call1(args)?;
        }
        Ok(())
    }

    fn rotated(&self, pyobj: &PyAny, gate: &str, theta: f64, qubit: String) -> PyResult<()> {
        let has_gate = pyobj.hasattr(gate)?;
        if has_gate {
            let func = pyobj.getattr(gate)?;
            let args = (theta, qubit);
            func.call1(args)?;
        }
        Ok(())
    }

    fn finish(&self, pyobj: &PyAny, dict: &PyDict) -> PyResult<()> {
        let has_gate = pyobj.hasattr("finish")?;
        if has_gate {
            let func = pyobj.getattr("finish")?;
            let args = (dict,);
            func.call1(args)?;
        }
        Ok(())
    }

    fn build_with_python(&self, pyobj: &PyAny) -> PyResult<()> {
        let result = qirlib::interop::emit::run(&self.model);
        if let Err(msg) = result {
            let err: PyErr = PyOSError::new_err::<String>(msg);
            return Err(err);
        }
        let gen_model = result.unwrap();
        Python::with_gil(|py| -> PyResult<()> {
            for instruction in gen_model.instructions {
                match instruction {
                    Instruction::Cx(ins) => {
                        self.controlled(pyobj, "cx", ins.control, ins.target)?
                    }
                    Instruction::Cz(ins) => {
                        self.controlled(pyobj, "cz", ins.control, ins.target)?
                    }
                    Instruction::H(ins) => self.single(pyobj, "h", ins.qubit)?,
                    Instruction::M(ins) => self.measured(pyobj, "m", ins.qubit, ins.target)?,
                    Instruction::Reset(_ins) => {
                        todo!("Not Implemented")
                    }
                    Instruction::Rx(ins) => self.rotated(pyobj, "rx", ins.theta, ins.qubit)?,
                    Instruction::Ry(ins) => self.rotated(pyobj, "ry", ins.theta, ins.qubit)?,
                    Instruction::Rz(ins) => self.rotated(pyobj, "rz", ins.theta, ins.qubit)?,
                    Instruction::S(ins) => self.single(pyobj, "s", ins.qubit)?,
                    Instruction::SAdj(ins) => self.single(pyobj, "s_adj", ins.qubit)?,
                    Instruction::T(ins) => self.single(pyobj, "t", ins.qubit)?,
                    Instruction::TAdj(ins) => self.single(pyobj, "t_adj", ins.qubit)?,
                    Instruction::X(ins) => self.single(pyobj, "x", ins.qubit)?,
                    Instruction::Y(ins) => self.single(pyobj, "y", ins.qubit)?,
                    Instruction::Z(ins) => self.single(pyobj, "z", ins.qubit)?,
                    Instruction::DumpMachine => {
                        todo!("Not Implemented")
                    }
                }
            }
            let dict = PyDict::new(py);
            dict.set_item("number_of_qubits", gen_model.qubits.len())?;
            self.finish(pyobj, dict)?;
            Ok(())
        })?;
        Ok(())
    }

    fn eval(&self, file: String, pyobj: &PyAny) -> PyResult<()> {
        let result = qirlib::interop::emit::run_module(file);
        if let Err(msg) = result {
            let err: PyErr = PyOSError::new_err::<String>(msg);
            return Err(err);
        }
        let gen_model = result.unwrap();
        Python::with_gil(|py| -> PyResult<()> {
            for instruction in gen_model.instructions {
                match instruction {
                    Instruction::Cx(ins) => {
                        self.controlled(pyobj, "cx", ins.control, ins.target)?
                    }
                    Instruction::Cz(ins) => {
                        self.controlled(pyobj, "cz", ins.control, ins.target)?
                    }
                    Instruction::H(ins) => self.single(pyobj, "h", ins.qubit)?,
                    Instruction::M(ins) => self.measured(pyobj, "m", ins.qubit, ins.target)?,
                    Instruction::Reset(_ins) => {
                        todo!("Not Implemented")
                    }
                    Instruction::Rx(ins) => self.rotated(pyobj, "rx", ins.theta, ins.qubit)?,
                    Instruction::Ry(ins) => self.rotated(pyobj, "ry", ins.theta, ins.qubit)?,
                    Instruction::Rz(ins) => self.rotated(pyobj, "rz", ins.theta, ins.qubit)?,
                    Instruction::S(ins) => self.single(pyobj, "s", ins.qubit)?,
                    Instruction::SAdj(ins) => self.single(pyobj, "s_adj", ins.qubit)?,
                    Instruction::T(ins) => self.single(pyobj, "t", ins.qubit)?,
                    Instruction::TAdj(ins) => self.single(pyobj, "t_adj", ins.qubit)?,
                    Instruction::X(ins) => self.single(pyobj, "x", ins.qubit)?,
                    Instruction::Y(ins) => self.single(pyobj, "y", ins.qubit)?,
                    Instruction::Z(ins) => self.single(pyobj, "z", ins.qubit)?,
                    Instruction::DumpMachine => {
                        todo!("Not Implemented")
                    }
                }
            }
            let dict = PyDict::new(py);
            dict.set_item("number_of_qubits", gen_model.qubits.len())?;
            self.finish(pyobj, dict)?;
            Ok(())
        })?;
        Ok(())
    }

    fn get_ir_string(&self) -> PyResult<String> {
        match qirlib::interop::emit::get_ir_string(&self.model) {
            Err(msg) => {
                let err: PyErr = PyOSError::new_err::<String>(msg);
                Err(err)
            }
            Ok(ir) => Ok(ir),
        }
    }

    fn get_bitcode_base64_string(&self) -> PyResult<String> {
        match qirlib::interop::emit::get_bitcode_base64_string(&self.model) {
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
