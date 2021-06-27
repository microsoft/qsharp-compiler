use pyo3::prelude::*;

use qirlib::{Emitter, Register, SemanticModel};

#[pymodule]
fn pyqir(_py: Python<'_>, m: &PyModule) -> PyResult<()> {
    m.add_class::<PyQIR>()?;

    Ok(())
}

#[pyclass]
pub struct PyQIR {
    model: SemanticModel,
}

#[pymethods]
impl PyQIR {
    #[new]
    fn new(name: String) -> Self {
        PyQIR {
            model: SemanticModel::new(name),
        }
    }

    fn add_measurement(&mut self, qubit: String, control: String) -> PyResult<()> {
        println!("measure {} => {}", qubit, control);
        Ok(())
    }

    fn add_quantum_register(&mut self, name: String, size: u64) -> PyResult<()> {
        let reg = Register::Quantum { name, size };
        self.model.add_reg(reg);
        Ok(())
    }

    fn add_classical_register(&mut self, name: String, size: u64) -> PyResult<()> {
        let ns = name.as_str();
        for index in 0..size {
            let register_name = format!("{}[{}]", ns, index);
            println!("Adding {}", register_name);
            let reg = Register::Classical {
                name: String::from(ns),
                index,
            };
            self.model.add_reg(reg);
        }
        Ok(())
    }

    fn write(&self, file_name: &str) -> PyResult<()> {
        Emitter::write(&self.model, file_name);

        Ok(())
    }

    fn todo(&self) -> PyResult<()> {
        todo!();
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn it_works() {
        let pyqir = PyQIR::new(String::from("name"));
        pyqir.write("module.ll").unwrap();
    }
}
