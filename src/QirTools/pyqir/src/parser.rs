// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// TODO(swernli): The initial version of the parser exposes a subset of the llvm_ir crate API into
// python directly, along with some extensions that provide QIR specific support (such as `get_qubit_static_id`).
// Eventually this should be split up similar to how QIR emission functionality works; these wrappers will
// remain here and provide the pyclass-compatible implementation, the QIR specific extensions will be implemented
// as traits and extended onto the llvm_ir types as part of the qirlib such that they can be conveniently used
// from within rust, and wrappers for each class and function will be added to __init__.py so that the 
// parser API can have full python doc comments for usability.

use pyo3::exceptions;
use pyo3::prelude::*;

use llvm_ir;
use llvm_ir::types::Typed;

#[pyclass]
pub struct QirModule {
    pub(super) module: llvm_ir::Module,
}

#[pyclass]
pub struct QirFunction {
    pub(super) function: llvm_ir::Function,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct QirParameter {
    pub(super) param: llvm_ir::function::Parameter,
}

#[pyclass]
pub struct QirBasicBlock {
    pub(super) block: llvm_ir::BasicBlock,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct QirInstruction {
    pub(super) instr: llvm_ir::instruction::Instruction,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct QirTerminator {
    pub(super) term: llvm_ir::terminator::Terminator,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct QirOperand {
    pub(super) op: llvm_ir::Operand,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct QirConstant {
    pub(super) constantref: llvm_ir::ConstantRef,
    pub(super) types: llvm_ir::types::Types,
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
                types: self.module.types.clone(),
            })
            .collect()
    }

    fn get_func_by_name(&self, name: String) -> PyResult<QirFunction> {
        match self.module.get_func_by_name(&name) {
            Some(f) => Ok(QirFunction {
                function: f.clone(),
                types: self.module.types.clone(),
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
                types: self.module.types.clone(),
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
            .map(|b| QirBasicBlock {
                block: b.clone(),
                types: self.types.clone(),
            })
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
            Some(b) => Ok(QirBasicBlock {
                block: b.clone(),
                types: self.types.clone(),
            }),
            None => Err(exceptions::PyTypeError::new_err(format!(
                "Block with name '{}' not found",
                name
            ))),
        }
    }

    fn get_instruction_by_output_name(&self, name: String) -> PyResult<QirInstruction> {
        for block in &self.function.basic_blocks {
            for instr in &block.instrs {
                match instr.try_get_result() {
                    Some(resname) => {
                        if name_to_string(resname) == name {
                            return Ok(QirInstruction {
                                instr: instr.clone(),
                                types: self.types.clone(),
                            });
                        }
                    }
                    None => continue,
                }
            }
        }
        Err(exceptions::PyTypeError::new_err(format!(
            "Instruction with result name '{}' not found",
            name
        )))
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
            .map(|i| QirInstruction {
                instr: i.clone(),
                types: self.types.clone(),
            })
            .collect()
    }

    #[getter]
    fn get_phi_nodes(&self) -> Vec<QirInstruction> {
        self.get_instructions()
            .into_iter()
            .filter(|i| i.get_is_phi())
            .collect()
    }

    fn get_phi_pairs_by_source_name(&self, name: String) -> Vec<(String, QirOperand)> {
        self.get_phi_nodes()
            .iter()
            .map(|i| {
                (
                    i.get_output_name().unwrap(),
                    i.get_phi_incoming_value_for_name(name.clone()),
                )
            })
            .filter(|phi_pair| phi_pair.1.is_ok())
            .map(|phi_pair| (phi_pair.0, phi_pair.1.unwrap()))
            .collect()
    }

    #[getter]
    fn get_terminator(&self) -> QirTerminator {
        QirTerminator {
            term: self.block.term.clone(),
            types: self.types.clone(),
        }
    }
}

macro_rules! instr_targets {
    ($qir_instr:ident, $instr_pat:pat, $match:ident, $name:literal) => {
        match &$qir_instr.instr {
            $instr_pat => Ok(vec![
                QirOperand {
                    op: $match.operand0.clone(),
                    types: $qir_instr.types.clone(),
                },
                QirOperand {
                    op: $match.operand1.clone(),
                    types: $qir_instr.types.clone(),
                },
            ]),
            _ => Err(exceptions::PyTypeError::new_err(format!(
                "Instruction is not {}.",
                $name
            ))),
        }
    };
}

#[pymethods]
impl QirInstruction {
    #[getter]
    fn get_is_add(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Add(_))
    }

    #[getter]
    fn get_add_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::Add(instr), instr, "add")
    }

    #[getter]
    fn get_is_sub(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Sub(_))
    }

    #[getter]
    fn get_sub_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::Sub(instr), instr, "sub")
    }

    #[getter]
    fn get_is_mul(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Mul(_))
    }

    #[getter]
    fn get_mul_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::Mul(instr), instr, "mul")
    }

    #[getter]
    fn get_is_udiv(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::UDiv(_))
    }

    #[getter]
    fn get_udiv_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::UDiv(instr), instr, "udiv")
    }

    #[getter]
    fn get_is_sdiv(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::SDiv(_))
    }

    #[getter]
    fn get_sdiv_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::SDiv(instr), instr, "sdiv")
    }

    #[getter]
    fn get_is_urem(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::URem(_))
    }

    #[getter]
    fn get_urem_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::URem(instr), instr, "urem")
    }

    #[getter]
    fn get_is_srem(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::SRem(_))
    }

    #[getter]
    fn get_srem_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::SRem(instr), instr, "srem")
    }

    #[getter]
    fn get_is_and(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::And(_))
    }

    #[getter]
    fn get_and_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::And(instr), instr, "and")
    }

    #[getter]
    fn get_is_or(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Or(_))
    }

    #[getter]
    fn get_or_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::Or(instr), instr, "or")
    }

    #[getter]
    fn get_is_xor(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Xor(_))
    }

    #[getter]
    fn get_xor_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::Xor(instr), instr, "xor")
    }

    #[getter]
    fn get_is_shl(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Shl(_))
    }

    #[getter]
    fn get_shl_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::Shl(instr), instr, "shl")
    }

    #[getter]
    fn get_is_lshr(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::LShr(_))
    }

    #[getter]
    fn get_lshr_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::LShr(instr), instr, "lshr")
    }

    #[getter]
    fn get_is_ashr(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::AShr(_))
    }

    #[getter]
    fn get_ashr_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::AShr(instr), instr, "ashr")
    }

    #[getter]
    fn get_is_fadd(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FAdd(_))
    }

    #[getter]
    fn get_fadd_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::FAdd(instr), instr, "fadd")
    }

    #[getter]
    fn get_is_fsub(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FSub(_))
    }

    #[getter]
    fn get_fsub_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::FSub(instr), instr, "fsub")
    }

    #[getter]
    fn get_is_fmul(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FMul(_))
    }

    #[getter]
    fn get_fmul_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::FMul(instr), instr, "fmul")
    }

    #[getter]
    fn get_is_fdiv(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FDiv(_))
    }

    #[getter]
    fn get_fdiv_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::FDiv(instr), instr, "fdiv")
    }

    #[getter]
    fn get_is_frem(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FRem(_))
    }

    #[getter]
    fn get_frem_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::FRem(instr), instr, "frem")
    }

    #[getter]
    fn get_is_fneg(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FNeg(_))
    }

    #[getter]
    fn get_fneg_targets(&self) -> PyResult<Vec<QirOperand>> {
        match &self.instr {
            llvm_ir::Instruction::FNeg(instr) => Ok(vec![QirOperand {
                op: instr.operand.clone(),
                types: self.types.clone(),
            }]),
            _ => Err(exceptions::PyTypeError::new_err("Instruction is not fneg.")),
        }
    }

    #[getter]
    fn get_is_extractelement(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::ExtractElement(_))
    }

    #[getter]
    fn get_is_insertelement(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::InsertElement(_))
    }

    #[getter]
    fn get_is_shufflevector(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::ShuffleVector(_))
    }

    #[getter]
    fn get_is_extractvalue(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::ExtractValue(_))
    }

    #[getter]
    fn get_is_insertvalue(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::InsertValue(_))
    }

    #[getter]
    fn get_is_alloca(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Alloca(_))
    }

    #[getter]
    fn get_is_load(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Load(_))
    }

    #[getter]
    fn get_is_store(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Store(_))
    }

    #[getter]
    fn get_is_getelementptr(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::GetElementPtr(_))
    }

    #[getter]
    fn get_is_trunc(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Trunc(_))
    }

    #[getter]
    fn get_is_zext(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::ZExt(_))
    }

    #[getter]
    fn get_is_sext(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::SExt(_))
    }

    #[getter]
    fn get_is_fptrunc(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FPTrunc(_))
    }

    #[getter]
    fn get_is_fpext(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FPExt(_))
    }

    #[getter]
    fn get_is_fptoui(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FPToUI(_))
    }

    #[getter]
    fn get_is_fptosi(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FPToSI(_))
    }

    #[getter]
    fn get_is_uitofp(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::UIToFP(_))
    }

    #[getter]
    fn get_is_sitofp(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::SIToFP(_))
    }

    #[getter]
    fn get_is_ptrtoint(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::PtrToInt(_))
    }

    #[getter]
    fn get_is_inttoptr(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::IntToPtr(_))
    }

    #[getter]
    fn get_is_bitcast(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::BitCast(_))
    }

    #[getter]
    fn get_is_addrspacecast(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::AddrSpaceCast(_))
    }

    #[getter]
    fn get_is_icmp(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::ICmp(_))
    }

    #[getter]
    fn get_icmp_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::ICmp(instr), instr, "icmp")
    }

    #[getter]
    fn get_is_fcmp(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FCmp(_))
    }

    #[getter]
    fn get_fcmp_targets(&self) -> PyResult<Vec<QirOperand>> {
        instr_targets!(self, llvm_ir::Instruction::FCmp(instr), instr, "fcmp")
    }

    #[getter]
    fn get_is_phi(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Phi(_))
    }

    #[getter]
    fn get_phi_incoming_values(&self) -> PyResult<Vec<(QirOperand, String)>> {
        match &self.instr {
            llvm_ir::Instruction::Phi(llvm_ir::instruction::Phi {
                incoming_values,
                dest: _,
                to_type: _,
                debugloc: _,
            }) => Ok(incoming_values
                .iter()
                .map(|(op, name)| {
                    (
                        QirOperand {
                            op: op.clone(),
                            types: self.types.clone(),
                        },
                        name_to_string(name),
                    )
                })
                .collect()),
            _ => Err(exceptions::PyTypeError::new_err("Instruction is not phi.")),
        }
    }

    fn get_phi_incoming_value_for_name(&self, name: String) -> PyResult<QirOperand> {
        match self
            .get_phi_incoming_values()?
            .into_iter()
            .find(|phi_pair| phi_pair.1 == name)
        {
            Some((op, _)) => Ok(op),
            None => Err(exceptions::PyTypeError::new_err(format!(
                "Phi instruction has no incoming value for block named '{}'",
                name
            ))),
        }
    }

    #[getter]
    fn get_is_select(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Select(_))
    }

    #[getter]
    fn get_is_call(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Call(_))
    }

    #[getter]
    fn get_call_func_name(&self) -> PyResult<String> {
        match &self.instr {
            llvm_ir::Instruction::Call(call) => match call.function.clone().right() {
                Some(llvm_ir::operand::Operand::ConstantOperand(cref)) => match cref.as_ref() {
                    llvm_ir::constant::Constant::GlobalReference { name, ty: _ } => {
                        Ok(name_to_string(&name))
                    }
                    _ => Err(exceptions::PyTypeError::new_err(
                        "Unhandled operand type in call.",
                    )),
                },
                _ => Err(exceptions::PyTypeError::new_err("Unhandled call type.")),
            },
            _ => Err(exceptions::PyTypeError::new_err("Instruction is not call.")),
        }
    }

    #[getter]
    fn get_call_func_params(&self) -> PyResult<Vec<QirOperand>> {
        match &self.instr {
            llvm_ir::Instruction::Call(call) => Ok(call
                .arguments
                .iter()
                .map(|o| QirOperand {
                    op: o.0.clone(),
                    types: self.types.clone(),
                })
                .collect()),
            _ => Err(exceptions::PyTypeError::new_err("Instruction is not call.")),
        }
    }

    #[getter]
    fn get_is_qis_call(&self) -> bool {
        match self.get_call_func_name() {
            Ok(name) => name.starts_with("__quantum__qis__"),
            _ => false,
        }
    }

    #[getter]
    fn get_is_qrt_call(&self) -> bool {
        match self.get_call_func_name() {
            Ok(name) => name.starts_with("__quantum__rt__"),
            _ => false,
        }
    }

    #[getter]
    fn get_is_qir_call(&self) -> bool {
        match self.get_call_func_name() {
            Ok(name) => name.starts_with("__quantum__qir__"),
            _ => false,
        }
    }

    #[getter]
    fn get_has_output(&self) -> bool {
        match self.instr.try_get_result() {
            Some(_) => true,
            None => false,
        }
    }

    #[getter]
    fn get_output_name(&self) -> PyResult<String> {
        match self.instr.try_get_result() {
            Some(name) => Ok(name_to_string(name)),
            None => Err(exceptions::PyTypeError::new_err(
                "Instruction has no output.",
            )),
        }
    }
}

#[pymethods]
impl QirTerminator {
    #[getter]
    fn get_is_ret(&self) -> bool {
        matches!(self.term, llvm_ir::Terminator::Ret(_))
    }

    #[getter]
    fn get_ret_operand(&self) -> PyResult<QirOperand> {
        match &self.term {
            llvm_ir::Terminator::Ret(llvm_ir::terminator::Ret {
                return_operand,
                debugloc: _,
            }) => match return_operand {
                Some(op) => Ok(QirOperand {
                    op: op.clone(),
                    types: self.types.clone(),
                }),
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
        matches!(self.term, llvm_ir::Terminator::Br(_))
    }

    #[getter]
    fn get_br_dest(&self) -> PyResult<String> {
        match &self.term {
            llvm_ir::Terminator::Br(llvm_ir::terminator::Br { dest, debugloc: _ }) => {
                Ok(name_to_string(&dest))
            }
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not branch.",
            )),
        }
    }

    #[getter]
    fn get_is_condbr(&self) -> bool {
        matches!(self.term, llvm_ir::Terminator::CondBr(_))
    }

    #[getter]
    fn get_condbr_condition(&self) -> PyResult<QirOperand> {
        match &self.term {
            llvm_ir::Terminator::CondBr(llvm_ir::terminator::CondBr {
                condition,
                true_dest: _,
                false_dest: _,
                debugloc: _,
            }) => Ok(QirOperand {
                op: condition.clone(),
                types: self.types.clone(),
            }),
            _ => Err(exceptions::PyTypeError::new_err(
                "Terminator is not condition branch.",
            )),
        }
    }

    #[getter]
    fn get_condbr_true_dest(&self) -> PyResult<String> {
        match &self.term {
            llvm_ir::Terminator::CondBr(llvm_ir::terminator::CondBr {
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
            llvm_ir::Terminator::CondBr(llvm_ir::terminator::CondBr {
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
        matches!(self.term, llvm_ir::Terminator::Switch(_))
    }

    #[getter]
    fn get_is_unreachable(&self) -> bool {
        matches!(self.term, llvm_ir::Terminator::Unreachable(_))
    }
}

#[pymethods]
impl QirOperand {
    #[getter]
    fn get_is_local(&self) -> bool {
        matches!(self.op, llvm_ir::Operand::LocalOperand { name: _, ty: _ })
    }

    #[getter]
    fn get_local_name(&self) -> PyResult<String> {
        match &self.op {
            llvm_ir::Operand::LocalOperand { name, ty: _ } => Ok(name_to_string(&name)),
            _ => Err(exceptions::PyTypeError::new_err("Operand is not local.")),
        }
    }

    #[getter]
    fn get_local_type(&self) -> PyResult<QirType> {
        match &self.op {
            llvm_ir::Operand::LocalOperand { name: _, ty } => Ok(QirType {
                typeref: ty.clone(),
            }),
            _ => Err(exceptions::PyTypeError::new_err("Operand is not local.")),
        }
    }

    #[getter]
    fn get_is_constant(&self) -> bool {
        matches!(self.op, llvm_ir::Operand::ConstantOperand(_))
    }

    #[getter]
    fn get_constant(&self) -> PyResult<QirConstant> {
        match &self.op {
            llvm_ir::Operand::ConstantOperand(cref) => Ok(QirConstant {
                constantref: cref.clone(),
                types: self.types.clone(),
            }),
            _ => Err(exceptions::PyTypeError::new_err("Operand is not constant.")),
        }
    }
}

#[pymethods]
impl QirConstant {
    #[getter]
    fn get_is_int(&self) -> bool {
        matches!(
            self.constantref.as_ref(),
            llvm_ir::Constant::Int { bits: _, value: _ }
        )
    }

    #[getter]
    fn get_int_value(&self) -> PyResult<i64> {
        match self.constantref.as_ref() {
            llvm_ir::Constant::Int { bits: _, value } => Ok(value.clone() as i64),
            _ => Err(exceptions::PyTypeError::new_err("Constant is not int.")),
        }
    }

    #[getter]
    fn get_int_width(&self) -> PyResult<u32> {
        match &self.constantref.as_ref() {
            llvm_ir::Constant::Int { bits, value: _ } => Ok(bits.clone()),
            _ => Err(exceptions::PyTypeError::new_err("Constant is not int.")),
        }
    }

    #[getter]
    fn get_is_float(&self) -> bool {
        matches!(self.constantref.as_ref(), llvm_ir::Constant::Float(_))
    }

    #[getter]
    fn get_float_double_value(&self) -> PyResult<f64> {
        match &self.constantref.as_ref() {
            llvm_ir::Constant::Float(f) => match f {
                llvm_ir::constant::Float::Double(d) => Ok(d.clone()),
                _ => Err(exceptions::PyTypeError::new_err(
                    "Constant is not float double.",
                )),
            },
            _ => Err(exceptions::PyTypeError::new_err("Constant is not float.")),
        }
    }

    #[getter]
    fn get_is_null(&self) -> bool {
        matches!(self.constantref.as_ref(), llvm_ir::Constant::Null(_))
    }

    #[getter]
    fn get_is_agregate_zero(&self) -> bool {
        matches!(
            self.constantref.as_ref(),
            llvm_ir::Constant::AggregateZero(_)
        )
    }

    #[getter]
    fn get_is_array(&self) -> bool {
        matches!(
            self.constantref.as_ref(),
            llvm_ir::Constant::Array {
                element_type: _,
                elements: _,
            }
        )
    }

    #[getter]
    fn get_is_vector(&self) -> bool {
        matches!(self.constantref.as_ref(), llvm_ir::Constant::Vector(_))
    }

    #[getter]
    fn get_is_undef(&self) -> bool {
        matches!(self.constantref.as_ref(), llvm_ir::Constant::Undef(_))
    }

    #[getter]
    fn get_is_global_reference(&self) -> bool {
        match self.constantref.as_ref() {
            llvm_ir::Constant::GlobalReference { name: _, ty: _ } => true,
            _ => false,
        }
    }

    #[getter]
    fn get_type(&self) -> QirType {
        QirType {
            typeref: self.constantref.get_type(&self.types),
        }
    }

    #[getter]
    fn get_is_qubit(&self) -> bool {
        self.get_type().get_is_qubit()
    }

    #[getter]
    fn get_qubit_static_id(&self) -> PyResult<u64> {
        if !self.get_is_qubit() {
            Err(exceptions::PyTypeError::new_err("Constant is not qubit."))
        } else {
            match &self.constantref.as_ref() {
                llvm_ir::Constant::Null(_) => Ok(0),
                llvm_ir::Constant::IntToPtr(llvm_ir::constant::IntToPtr {
                    operand,
                    to_type: _,
                }) => match operand.as_ref() {
                    llvm_ir::Constant::Int { bits: 64, value } => Ok(value.clone()),
                    _ => Err(exceptions::PyTypeError::new_err(
                        "Qubit is not recognized constant.",
                    )),
                },
                _ => Err(exceptions::PyTypeError::new_err(
                    "Qubit is not recognized constant.",
                )),
            }
        }
    }

    #[getter]
    fn get_is_result(&self) -> bool {
        self.get_type().get_is_result()
    }

    #[getter]
    fn get_result_static_id(&self) -> PyResult<u64> {
        if !self.get_is_result() {
            Err(exceptions::PyTypeError::new_err("Constant is not result."))
        } else {
            match &self.constantref.as_ref() {
                llvm_ir::Constant::Null(_) => Ok(0),
                llvm_ir::Constant::IntToPtr(llvm_ir::constant::IntToPtr {
                    operand,
                    to_type: _,
                }) => match operand.as_ref() {
                    llvm_ir::Constant::Int { bits: 64, value } => Ok(value.clone()),
                    _ => Err(exceptions::PyTypeError::new_err(
                        "Result is not recognized constant.",
                    )),
                },
                _ => Err(exceptions::PyTypeError::new_err(
                    "Result is not recognized constant.",
                )),
            }
        }
    }
}

#[pymethods]
impl QirType {
    #[getter]
    fn get_is_void(&self) -> bool {
        matches!(self.typeref.as_ref(), llvm_ir::Type::VoidType)
    }

    #[getter]
    fn get_is_integer(&self) -> bool {
        matches!(
            self.typeref.as_ref(),
            llvm_ir::Type::IntegerType { bits: _ }
        )
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
        matches!(
            self.typeref.as_ref(),
            llvm_ir::Type::PointerType {
                pointee_type: _,
                addr_space: _,
            }
        )
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
        matches!(
            self.typeref.as_ref(),
            llvm_ir::Type::FPType(llvm_ir::types::FPType::Double)
        )
    }

    #[getter]
    fn get_is_array(&self) -> bool {
        matches!(
            self.typeref.as_ref(),
            llvm_ir::Type::ArrayType {
                element_type: _,
                num_elements: _,
            }
        )
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
        matches!(
            self.typeref.as_ref(),
            llvm_ir::Type::StructType {
                element_types: _,
                is_packed: _,
            }
        )
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
        matches!(
            self.typeref.as_ref(),
            llvm_ir::Type::NamedStructType { name: _ }
        )
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
        llvm_ir::Name::Name(n) => n.to_string(),
        llvm_ir::Name::Number(n) => n.to_string(),
    }
}
