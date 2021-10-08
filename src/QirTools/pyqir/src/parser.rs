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

#[pyclass]
pub struct QirOperand {
    pub(super) op: llvm_ir::Operand,
}

#[pyclass]
pub struct QirConstant {
    pub(super) constantref: llvm_ir::ConstantRef,
}

#[pyclass]
pub struct QirType {
    pub(super) typeref: llvm_ir::TypeRef,
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
impl QirBasicBlock {
    #[getter]
    fn get_name(&self) -> String {
        name_to_string(&self.block.name)
    }

    #[getter]
    fn get_instructions(&self) -> Vec<QirInstruction> {
        self.block
            .instrs
            .iter()
            .map(|i| QirInstruction { instr: i.clone() })
            .collect()
    }

    #[getter]
    fn get_terminator(&self) -> QirTerminator {
        QirTerminator {
            term: self.block.term.clone(),
        }
    }
}

#[pymethods]
impl QirInstruction {
    #[getter]
    fn get_is_add(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Add(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_sub(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Sub(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_mul(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Mul(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_udiv(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::UDiv(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_sdiv(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::SDiv(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_urem(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::URem(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_srem(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::SRem(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_and(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::And(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_or(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Or(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_xor(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Xor(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_shl(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Shl(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_lshr(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::LShr(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_ashr(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::AShr(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fadd(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FAdd(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fsub(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FSub(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fmul(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FMul(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fdiv(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FDiv(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_frem(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FRem(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fneg(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FNeg(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_extractelement(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::ExtractElement(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_insertelement(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::InsertElement(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_shufflevector(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::ShuffleVector(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_extractvalue(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::ExtractValue(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_insertvalue(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::InsertValue(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_alloca(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Alloca(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_load(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Load(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_store(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Store(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_getelementptr(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::GetElementPtr(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_trunc(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Trunc(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_zext(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::ZExt(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_sext(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::SExt(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fptrunc(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FPTrunc(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fpext(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FPExt(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fptoui(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FPToUI(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fptosi(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FPToSI(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_uitofp(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::UIToFP(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_sitofp(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::SIToFP(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_ptrtoint(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::PtrToInt(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_inttoptr(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::IntToPtr(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_bitcast(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::BitCast(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_addrspacecast(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::AddrSpaceCast(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_icmp(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::ICmp(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_fcmp(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::FCmp(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_phi(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Phi(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_select(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Select(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_is_call(&self) -> bool {
        match self.instr {
            llvm_ir::instruction::Instruction::Call(_) => true,
            _ => false,
        }
    }
}

#[pymethods]
impl QirTerminator {
    #[getter]
    fn get_is_ret(&self) -> bool {
        match self.term {
            llvm_ir::terminator::Terminator::Ret(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_ret_operand(&self) -> PyResult<QirOperand> {
        match &self.term {
            llvm_ir::terminator::Terminator::Ret(llvm_ir::terminator::Ret {
                return_operand,
                debugloc: _,
            }) => match return_operand {
                Some(op) => Ok(QirOperand { op: op.clone() }),
                None => Err(exceptions::PyTypeError::new_err(
                    "Return is void and has no operand.",
                )),
            },
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not return.",
            )),
        }
    }

    #[getter]
    fn get_is_br(&self) -> bool {
        match self.term {
            llvm_ir::terminator::Terminator::Br(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_br_dest(&self) -> PyResult<String> {
        match &self.term {
            llvm_ir::terminator::Terminator::Br(llvm_ir::terminator::Br { dest, debugloc: _ }) => {
                Ok(name_to_string(&dest))
            }
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not branch.",
            )),
        }
    }

    #[getter]
    fn get_is_condbr(&self) -> bool {
        match self.term {
            llvm_ir::terminator::Terminator::CondBr(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_condbr_condition(&self) -> PyResult<QirOperand> {
        match &self.term {
            llvm_ir::terminator::Terminator::CondBr(llvm_ir::terminator::CondBr {
                condition,
                true_dest: _,
                false_dest: _,
                debugloc: _,
            }) => Ok(QirOperand {
                op: condition.clone(),
            }),
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not condition branch.",
            )),
        }
    }

    #[getter]
    fn get_condbr_true_dest(&self) -> PyResult<String> {
        match &self.term {
            llvm_ir::terminator::Terminator::CondBr(llvm_ir::terminator::CondBr {
                condition: _,
                true_dest,
                false_dest: _,
                debugloc: _,
            }) => Ok(name_to_string(&true_dest)),
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not condition branch.",
            )),
        }
    }

    #[getter]
    fn get_condbr_false_dest(&self) -> PyResult<String> {
        match &self.term {
            llvm_ir::terminator::Terminator::CondBr(llvm_ir::terminator::CondBr {
                condition: _,
                true_dest: _,
                false_dest,
                debugloc: _,
            }) => Ok(name_to_string(&false_dest)),
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not condition branch.",
            )),
        }
    }

    #[getter]
    fn get_is_switch(&self) -> bool {
        match self.term {
            llvm_ir::terminator::Terminator::Switch(_) => true,
            _ => false,
        }
    }

    #[getter]
    fn get_switch_operand(&self) -> PyResult<QirOperand> {
        match self.term {
            llvm_ir::terminator::Terminator::Switch(_) => Err(exceptions::PyTypeError::new_err(
                "Switch handling not supported.",
            )),
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not switch.",
            )),
        }
    }

    #[getter]
    fn get_switch_dest_values(&self) -> PyResult<Vec<QirConstant>> {
        match self.term {
            llvm_ir::terminator::Terminator::Switch(_) => Err(exceptions::PyTypeError::new_err(
                "Switch handling not supported.",
            )),
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not switch.",
            )),
        }
    }

    #[getter]
    fn get_switch_dest_names(&self) -> PyResult<Vec<String>> {
        match self.term {
            llvm_ir::terminator::Terminator::Switch(_) => Err(exceptions::PyTypeError::new_err(
                "Switch handling not supported.",
            )),
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not switch.",
            )),
        }
    }

    #[getter]
    fn get_is_unreachable(&self) -> bool {
        match self.term {
            llvm_ir::terminator::Terminator::Unreachable(_) => true,
            _ => false,
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
    fn get_integer_width(&self) -> PyResult<u32> {
        match self.typeref.as_ref() {
            llvm_ir::Type::IntegerType { bits } => Ok(bits.clone()),
            _ => Err(exceptions::PyTypeError::new_err("Type is not integer.")),
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
    fn get_pointer_type(&self) -> PyResult<QirType> {
        match self.typeref.as_ref() {
            llvm_ir::Type::PointerType {
                pointee_type,
                addr_space: _,
            } => Ok(QirType {
                typeref: pointee_type.clone(),
            }),
            _ => Err(exceptions::PyTypeError::new_err("Type is not pointer.")),
        }
    }

    #[getter]
    fn get_pointer_addrspace(&self) -> PyResult<u32> {
        match self.typeref.as_ref() {
            llvm_ir::Type::PointerType {
                pointee_type: _,
                addr_space,
            } => Ok(addr_space.clone()),
            _ => Err(exceptions::PyTypeError::new_err("Type is not pointer.")),
        }
    }

    #[getter]
    fn get_is_double(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::FPType(llvm_ir::types::FPType::Double) => true,
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
    fn get_array_element_type(&self) -> PyResult<QirType> {
        match self.typeref.as_ref() {
            llvm_ir::Type::ArrayType {
                element_type,
                num_elements: _,
            } => Ok(QirType {
                typeref: element_type.clone(),
            }),
            _ => Err(exceptions::PyTypeError::new_err("Type is not array.")),
        }
    }

    #[getter]
    fn get_array_num_elements(&self) -> PyResult<usize> {
        match self.typeref.as_ref() {
            llvm_ir::Type::ArrayType {
                element_type: _,
                num_elements,
            } => Ok(num_elements.clone()),
            _ => Err(exceptions::PyTypeError::new_err("Type is not array.")),
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
    fn get_struct_element_types(&self) -> PyResult<Vec<QirType>> {
        match self.typeref.as_ref() {
            llvm_ir::Type::StructType {
                element_types,
                is_packed: _,
            } => Ok(element_types
                .iter()
                .map(|t| QirType { typeref: t.clone() })
                .collect()),
            _ => Err(exceptions::PyTypeError::new_err("Type is not struct.")),
        }
    }

    #[getter]
    fn get_is_named_struct(&self) -> bool {
        match self.typeref.as_ref() {
            llvm_ir::Type::NamedStructType { name: _ } => true,
            _ => false,
        }
    }

    #[getter]
    fn get_named_struct_name(&self) -> PyResult<String> {
        match self.typeref.as_ref() {
            llvm_ir::Type::NamedStructType { name } => Ok(name.clone()),
            _ => Err(exceptions::PyTypeError::new_err(
                "Type is not named struct.",
            )),
        }
    }

    #[getter]
    fn get_is_qubit(&self) -> bool {
        self.get_is_pointer()
            && self.get_pointer_type().unwrap().get_is_named_struct()
            && self
                .get_pointer_type()
                .unwrap()
                .get_named_struct_name()
                .unwrap()
                == "Qubit"
    }

    #[getter]
    fn get_is_result(&self) -> bool {
        self.get_is_pointer()
            && self.get_pointer_type().unwrap().get_is_named_struct()
            && self
                .get_pointer_type()
                .unwrap()
                .get_named_struct_name()
                .unwrap()
                == "Result"
    }
}

fn name_to_string(name: &llvm_ir::Name) -> String {
    match name {
        llvm_ir::name::Name::Name(n) => n.to_string(),
        llvm_ir::name::Name::Number(n) => n.to_string(),
    }
}
