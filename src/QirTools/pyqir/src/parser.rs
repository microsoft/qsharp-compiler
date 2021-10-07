// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
use pyo3::exceptions;
use pyo3::prelude::*;

use llvm_ir;

#[pyclass]
pub struct QirModule {
    pub(super) module: llvm_ir::Module,
}

#[pyclass]
pub struct QirFunction {
    pub(super) function: llvm_ir::Function,
}

#[pyclass]
pub struct QirParameter {
    pub(super) param: llvm_ir::function::Parameter,
}

#[pyclass]
pub struct QirType {
    pub(super) typeref: llvm_ir::TypeRef,
}

#[pyclass]
pub struct QirFunctionAttribute {
    pub(super) attr: llvm_ir::function::FunctionAttribute,
}

#[pyclass]
pub struct QirBasicBlock {
    pub(super) block: llvm_ir::BasicBlock,
}

#[pyclass]
pub struct QirInstruction {
    pub(super) instr: llvm_ir::instruction::Instruction,
}

#[pyclass]
pub struct QirTerminator {
    pub(super) term: llvm_ir::terminator::Terminator,
}

#[pymethods]
impl QirModule {
    #[getter]
    fn get_functions(&self) -> Vec<QirFunction> {
        self.module
            .functions
            .iter()
            .map(|f| QirFunction {
                function: f.clone(),
            })
            .collect()
    }

    fn get_func_by_name(&self, name: String) -> PyResult<QirFunction> {
        match self.module.get_func_by_name(&name) {
            Some(f) => Ok(QirFunction {
                function: f.clone(),
            }),
            None => Err(exceptions::PyTypeError::new_err(format!(
                "Function with name '{}' not found",
                name
            ))),
        }
    }

    fn get_funcs_by_attr(&self, attr: String) -> Vec<QirFunction> {
        self.module
            .functions
            .iter()
            .filter(|f| {
                f.function_attributes.contains(
                    &llvm_ir::function::FunctionAttribute::StringAttribute {
                        kind: attr.clone(),
                        value: "".to_string(),
                    },
                )
            })
            .map(|f| QirFunction {
                function: f.clone(),
            })
            .collect()
    }
}

#[pymethods]
impl QirFunction {
    #[getter]
    fn get_name(&self) -> String {
        self.function.name.clone()
    }

    #[getter]
    fn get_parameters(&self) -> Vec<QirParameter> {
        self.function
            .parameters
            .iter()
            .map(|p| QirParameter { param: p.clone() })
            .collect()
    }

    #[getter]
    fn get_return_type(&self) -> QirType {
        QirType {
            typeref: self.function.return_type.clone(),
        }
    }

    #[getter]
    fn get_attributes(&self) -> Vec<QirFunctionAttribute> {
        self.function
            .function_attributes
            .iter()
            .map(|a| QirFunctionAttribute { attr: a.clone() })
            .collect()
    }

    #[getter]
    fn get_blocks(&self) -> Vec<QirBasicBlock> {
        self.function
            .basic_blocks
            .iter()
            .map(|b| QirBasicBlock { block: b.clone() })
            .collect()
    }

    #[getter]
    fn get_required_qubits(&self) -> i64 {
        match self.get_attribute_value("requiredQubits".to_string()) {
            Ok(s) => s.parse().unwrap(),
            Err(_) => 0,
        }
    }

    #[getter]
    fn get_required_results(&self) -> i64 {
        match self.get_attribute_value("requiredResults".to_string()) {
            Ok(s) => s.parse().unwrap(),
            Err(_) => 0,
        }
    }

    fn get_attribute_value(&self, attr_name: String) -> PyResult<String> {
        for attr in &self.function.function_attributes {
            match attr {
                llvm_ir::function::FunctionAttribute::StringAttribute { kind, value } => {
                    if kind.to_string() == attr_name {
                        return Ok(value.to_string());
                    }
                }
                _ => continue,
            }
        }
        Err(exceptions::PyTypeError::new_err(format!(
            "Attribute with name '{}' not found",
            attr_name
        )))
    }

    fn get_block_by_name(&self, name: String) -> PyResult<QirBasicBlock> {
        match self
            .function
            .get_bb_by_name(&llvm_ir::Name::from(name.clone()))
        {
            Some(b) => Ok(QirBasicBlock { block: b.clone() }),
            None => Err(exceptions::PyTypeError::new_err(format!(
                "Block with name '{}' not found",
                name
            ))),
        }
    }
}

#[pymethods]
impl QirParameter {
    #[getter]
    fn get_name(&self) -> String {
        name_to_string(&self.param.name)
    }

    #[getter]
    fn get_type(&self) -> QirType {
        QirType {
            typeref: self.param.ty.clone(),
        }
    }
}

#[pymethods]
impl QirType {
    #[getter]
    fn get_is_void(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::VoidType => true,
            _ => false,
        }
    }
    #[getter]
    fn get_is_integer(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::IntegerType { bits: _ } => true,
            _ => false,
        }
    }
    #[getter]
    fn get_is_pointer(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::PointerType {
                pointee_type: _,
                addr_space: _,
            } => true,
            _ => false,
        }
    }
    #[getter]
    fn get_is_floating_point(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::FPType(_) => true,
            _ => false,
        }
    }
    #[getter]
    fn get_is_func(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::FuncType {
                result_type: _,
                param_types: _,
                is_var_arg: _,
            } => true,
            _ => false,
        }
    }
    #[getter]
    fn get_is_vector(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::VectorType {
                element_type: _,
                num_elements: _,
                scalable: _,
            } => true,
            _ => false,
        }
    }
    #[getter]
    fn get_is_array(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::ArrayType {
                element_type: _,
                num_elements: _,
            } => true,
            _ => false,
        }
    }
    #[getter]
    fn get_is_struct(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::StructType {
                element_types: _,
                is_packed: _,
            } => true,
            _ => false,
        }
    }
    #[getter]
    fn get_is_named_struct(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::NamedStructType { name: _ } => true,
            _ => false,
        }
    }
}

fn name_to_string(name: &llvm_ir::Name) -> String {
    match name {
        llvm_ir::name::Name::Name(n) => n.to_string(),
        llvm_ir::name::Name::Number(n) => n.to_string(),
    }
}
