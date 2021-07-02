use pyo3::prelude::*;

use qirlib::emit::Emitter;
use qirlib::interop::{Instruction, Register, SemanticModel};

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
        println!("measure {} => {}", qubit, target);
        Ok(())
    }

    fn cx(&mut self, control: String, target: String) -> PyResult<()> {
        println!("cx {} => {}", control, target);
        let inst = Instruction::Cx { control, target };
        self.model.add_inst(inst);
        Ok(())
    }

    fn cz(&mut self, control: String, target: String) -> PyResult<()> {
        println!("cz {} => {}", control, target);
        let inst = Instruction::Cz { control, target };
        self.model.add_inst(inst);
        Ok(())
    }

    fn h(&mut self, qubit: String) -> PyResult<()> {
        println!("h => {}", qubit);
        let inst = Instruction::H(qubit);
        self.model.add_inst(inst);
        Ok(())
    }

    fn m(&mut self, qubit: String, target: String) -> PyResult<()> {
        println!("m {}[{}]", qubit, target);
        let inst = Instruction::M { qubit, target };
        self.model.add_inst(inst);
        Ok(())
    }

    fn reset(&mut self, qubit: String) -> PyResult<()> {
        println!("reset => {}", qubit);
        let inst = Instruction::Reset(qubit);
        self.model.add_inst(inst);
        Ok(())
    }

    fn rx(&mut self, theta: f64, qubit: String) -> PyResult<()> {
        println!("rx {} => {}", qubit, theta);
        let inst = Instruction::Rx { theta, qubit };
        self.model.add_inst(inst);
        Ok(())
    }

    fn ry(&mut self, theta: f64, qubit: String) -> PyResult<()> {
        println!("ry {} => {}", qubit, theta);
        let inst = Instruction::Ry { theta, qubit };
        self.model.add_inst(inst);
        Ok(())
    }

    fn rz(&mut self, theta: f64, qubit: String) -> PyResult<()> {
        println!("rz {} => {}", qubit, theta);
        let inst = Instruction::Rz { theta, qubit };
        self.model.add_inst(inst);
        Ok(())
    }

    fn s(&mut self, qubit: String) -> PyResult<()> {
        println!("s => {}", qubit);
        let inst = Instruction::S(qubit);
        self.model.add_inst(inst);
        Ok(())
    }

    fn s_adj(&mut self, qubit: String) -> PyResult<()> {
        println!("s_adj => {}", qubit);
        let inst = Instruction::Sdg(qubit);
        self.model.add_inst(inst);
        Ok(())
    }

    fn t(&mut self, qubit: String) -> PyResult<()> {
        println!("t => {}", qubit);
        let inst = Instruction::T(qubit);
        self.model.add_inst(inst);
        Ok(())
    }

    fn t_adj(&mut self, qubit: String) -> PyResult<()> {
        println!("t_adj => {}", qubit);
        let inst = Instruction::Tdg(qubit);
        self.model.add_inst(inst);
        Ok(())
    }

    fn x(&mut self, qubit: String) -> PyResult<()> {
        println!("x => {}", qubit);
        let inst = Instruction::X(qubit);
        self.model.add_inst(inst);
        Ok(())
    }

    fn y(&mut self, qubit: String) -> PyResult<()> {
        println!("y => {}", qubit);
        let inst = Instruction::Y(qubit);
        self.model.add_inst(inst);
        Ok(())
    }

    fn z(&mut self, qubit: String) -> PyResult<()> {
        println!("z => {}", qubit);
        let inst = Instruction::Z(qubit);
        self.model.add_inst(inst);
        Ok(())
    }

    fn add_quantum_register(&mut self, name: String, size: u64) -> PyResult<()> {
        let ns = name.as_str();
        for index in 0..size {
            let register_name = format!("{}[{}]", ns, index);
            println!("Adding {}", register_name);
            let reg = Register::Quantum {
                name: String::from(ns),
                index,
            };
            self.model.add_reg(reg);
        }
        Ok(())
    }

    fn add_classical_register(&mut self, name: String, size: u64) -> PyResult<()> {
        let ns = name.clone();
        let reg = Register::Classical { name, size };
        println!("Adding {}({})", ns, size);
        self.model.add_reg(reg);
        Ok(())
    }

    fn write(&self, file_name: &str) -> PyResult<()> {
        Emitter::write(&self.model, file_name);
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn e2e_smoke() {
        let pyqir = PyQIR::new(String::from("name"));
        pyqir.write("module.ll").unwrap();
    }

    #[test]
    fn bell_measure() -> PyResult<()> {
        let mut pyqir = PyQIR::new(String::from("Bell circuit"));
        pyqir.add_quantum_register(String::from("qr"), 2)?;
        pyqir.add_classical_register(String::from("qc"), 2)?;
        pyqir.h(String::from("qr0"))?;
        pyqir.cx(String::from("qr0"), String::from("qr1"))?;
        pyqir.add_measurement(String::from("qr0"), String::from("qc0"))?;
        pyqir.add_measurement(String::from("qr1"), String::from("qc1"))?;
        pyqir.write("bell_measure.ll")?;
        Ok(())
    }

    #[test]
    fn bell_no_measure() -> PyResult<()> {
        let mut pyqir = PyQIR::new(String::from("Bell circuit"));
        pyqir.add_quantum_register(String::from("qr"), 2)?;
        pyqir.add_classical_register(String::from("qc"), 2)?;
        pyqir.h(String::from("qr0"))?;
        pyqir.cx(String::from("qr0"), String::from("qr1"))?;
        pyqir.write("bell_measure.ll")?;
        Ok(())
    }
}
