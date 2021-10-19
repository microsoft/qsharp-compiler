// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// TODO(swernli): The initial version of the parser exposes a subset of the llvm_ir crate API into
// python directly, along with some extensions that provide QIR specific support (such as `get_qubit_static_id`).
// Eventually this should be split up similar to how QIR emission functionality works; these wrappers will
// remain here and provide the pyclass-compatible implementation, the QIR specific extensions will be implemented
// as traits and extended onto the llvm_ir types as part of the qirlib such that they can be conveniently used
// from within rust, and wrappers for each class and function will be added to __init__.py so that the
// parser API can have full python doc comments for usability.

use llvm_ir;
use llvm_ir::types::Typed;
use pyo3::prelude::*;
use qirlib::parse::*;
use std::convert::TryFrom;

#[pyclass]
pub struct PyQirModule {
    pub(super) module: llvm_ir::Module,
}

#[pyclass]
pub struct PyQirFunction {
    pub(super) function: llvm_ir::Function,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct PyQirParameter {
    pub(super) param: llvm_ir::function::Parameter,
}

#[pyclass]
pub struct PyQirBasicBlock {
    pub(super) block: llvm_ir::BasicBlock,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct PyQirInstruction {
    pub(super) instr: llvm_ir::instruction::Instruction,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct PyQirTerminator {
    pub(super) term: llvm_ir::terminator::Terminator,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct PyQirOperand {
    pub(super) op: llvm_ir::Operand,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct PyQirConstant {
    pub(super) constantref: llvm_ir::ConstantRef,
    pub(super) types: llvm_ir::types::Types,
}

#[pyclass]
pub struct PyQirType {
    pub(super) typeref: llvm_ir::TypeRef,
}

macro_rules! match_contents {
    ($target:expr, $pattern:pat, $property:expr) => {
        match $target {
            $pattern => Some($property),
            _ => None,
        }
    };
}

#[pymethods]
impl PyQirModule {
    #[getter]
    fn get_functions(&self) -> Vec<PyQirFunction> {
        self.module
            .functions
            .iter()
            .map(|f| PyQirFunction {
                function: f.clone(),
                types: self.module.types.clone(),
            })
            .collect()
    }

    fn get_func_by_name(&self, name: String) -> Option<PyQirFunction> {
        match self.module.get_func_by_name(&name) {
            Some(f) => Some(PyQirFunction {
                function: f.clone(),
                types: self.module.types.clone(),
            }),
            None => None,
        }
    }

    fn get_funcs_by_attr(&self, attr: String) -> Vec<PyQirFunction> {
        self.module
            .get_func_by_attr_name(&attr)
            .iter()
            .map(|f| PyQirFunction {
                function: (*f).clone(),
                types: self.module.types.clone(),
            })
            .collect()
    }

    fn get_entrypoint_funcs(&self) -> Vec<PyQirFunction> {
        self.module
            .get_entrypoint_funcs()
            .iter()
            .map(|f| PyQirFunction {
                function: (*f).clone(),
                types: self.module.types.clone(),
            })
            .collect()
    }

    fn get_interop_funcs(&self) -> Vec<PyQirFunction> {
        self.module
            .get_interop_funcs()
            .iter()
            .map(|f| PyQirFunction {
                function: (*f).clone(),
                types: self.module.types.clone(),
            })
            .collect()
    }
}

#[pymethods]
impl PyQirFunction {
    #[getter]
    fn get_name(&self) -> String {
        self.function.name.clone()
    }

    #[getter]
    fn get_parameters(&self) -> Vec<PyQirParameter> {
        self.function
            .parameters
            .iter()
            .map(|p| PyQirParameter { param: p.clone() })
            .collect()
    }

    #[getter]
    fn get_return_type(&self) -> PyQirType {
        PyQirType {
            typeref: self.function.return_type.clone(),
        }
    }

    #[getter]
    fn get_blocks(&self) -> Vec<PyQirBasicBlock> {
        self.function
            .basic_blocks
            .iter()
            .map(|b| PyQirBasicBlock {
                block: b.clone(),
                types: self.types.clone(),
            })
            .collect()
    }

    #[getter]
    fn get_required_qubits(&self) -> PyResult<Option<i64>> {
        Ok(self.function.get_required_qubits()?)
    }

    #[getter]
    fn get_required_results(&self) -> PyResult<Option<i64>> {
        Ok(self.function.get_required_results()?)
    }

    fn get_attribute_value(&self, attr_name: String) -> Option<String> {
        self.function.get_attribute_value(&attr_name)
    }

    fn get_block_by_name(&self, name: String) -> Option<PyQirBasicBlock> {
        Some(PyQirBasicBlock {
            block: self
                .function
                .get_bb_by_name(&llvm_ir::Name::from(name.clone()))?
                .clone(),
            types: self.types.clone(),
        })
    }

    fn get_instruction_by_output_name(&self, name: String) -> Option<PyQirInstruction> {
        Some(PyQirInstruction {
            instr: self.function.get_instruction_by_output_name(&name)?.clone(),
            types: self.types.clone(),
        })
    }
}

#[pymethods]
impl PyQirParameter {
    #[getter]
    fn get_name(&self) -> String {
        self.param.name.get_string()
    }

    #[getter]
    fn get_type(&self) -> PyQirType {
        PyQirType {
            typeref: self.param.ty.clone(),
        }
    }
}

#[pymethods]
impl PyQirBasicBlock {
    #[getter]
    fn get_name(&self) -> String {
        self.block.name.get_string()
    }

    #[getter]
    fn get_instructions(&self) -> Vec<PyQirInstruction> {
        self.block
            .instrs
            .iter()
            .map(|i| PyQirInstruction {
                instr: i.clone(),
                types: self.types.clone(),
            })
            .collect()
    }

    #[getter]
    fn get_phi_nodes(&self) -> Vec<PyQirInstruction> {
        self.block
            .get_phi_nodes()
            .iter()
            .map(|phi| PyQirInstruction {
                instr: llvm_ir::Instruction::from(phi.clone()),
                types: self.types.clone(),
            })
            .collect()
    }

    fn get_phi_pairs_by_source_name(&self, name: String) -> Vec<(String, PyQirOperand)> {
        self.block
            .get_phi_pairs_by_source_name(&name)
            .iter()
            .map(|(n, op)| {
                (
                    n.get_string(),
                    PyQirOperand {
                        op: op.clone(),
                        types: self.types.clone(),
                    },
                )
            })
            .collect()
    }

    #[getter]
    fn get_terminator(&self) -> PyQirTerminator {
        PyQirTerminator {
            term: self.block.term.clone(),
            types: self.types.clone(),
        }
    }
}

#[pymethods]
impl PyQirInstruction {
    #[getter]
    fn get_target_operands(&self) -> Vec<PyQirOperand> {
        self.instr
            .get_target_operands()
            .iter()
            .map(|op| PyQirOperand {
                op: op.clone(),
                types: self.types.clone(),
            })
            .collect()
    }

    #[getter]
    fn get_type(&self) -> Option<PyQirType> {
        Some(PyQirType {
            typeref: self.instr.get_type(&self.types),
        })
    }

    #[getter]
    fn get_is_add(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Add(_))
    }

    #[getter]
    fn get_is_sub(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Sub(_))
    }

    #[getter]
    fn get_is_mul(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Mul(_))
    }

    #[getter]
    fn get_is_udiv(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::UDiv(_))
    }

    #[getter]
    fn get_is_sdiv(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::SDiv(_))
    }

    #[getter]
    fn get_is_urem(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::URem(_))
    }

    #[getter]
    fn get_is_srem(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::SRem(_))
    }

    #[getter]
    fn get_is_and(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::And(_))
    }

    #[getter]
    fn get_is_or(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Or(_))
    }

    #[getter]
    fn get_is_xor(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Xor(_))
    }

    #[getter]
    fn get_is_shl(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Shl(_))
    }

    #[getter]
    fn get_is_lshr(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::LShr(_))
    }

    #[getter]
    fn get_is_ashr(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::AShr(_))
    }

    #[getter]
    fn get_is_fadd(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FAdd(_))
    }

    #[getter]
    fn get_is_fsub(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FSub(_))
    }

    #[getter]
    fn get_is_fmul(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FMul(_))
    }

    #[getter]
    fn get_is_fdiv(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FDiv(_))
    }

    #[getter]
    fn get_is_frem(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FRem(_))
    }

    #[getter]
    fn get_is_fneg(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FNeg(_))
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
    fn get_is_fcmp(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::FCmp(_))
    }

    #[getter]
    fn get_is_phi(&self) -> bool {
        matches!(self.instr, llvm_ir::Instruction::Phi(_))
    }

    #[getter]
    fn get_phi_incoming_values(&self) -> Option<Vec<(PyQirOperand, String)>> {
        Some(
            llvm_ir::instruction::Phi::try_from(self.instr.clone())
                .ok()?
                .incoming_values
                .iter()
                .map(|(op, name)| {
                    (
                        PyQirOperand {
                            op: op.clone(),
                            types: self.types.clone(),
                        },
                        name.get_string(),
                    )
                })
                .collect(),
        )
    }

    fn get_phi_incoming_value_for_name(&self, name: String) -> Option<PyQirOperand> {
        Some(PyQirOperand {
            op: llvm_ir::instruction::Phi::try_from(self.instr.clone())
                .ok()?
                .get_incoming_value_for_name(&name)?,
            types: self.types.clone(),
        })
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
    fn get_call_func_name(&self) -> Option<String> {
        Some(
            llvm_ir::instruction::Call::try_from(self.instr.clone())
                .ok()?
                .get_func_name()?
                .get_string(),
        )
    }

    #[getter]
    fn get_call_func_params(&self) -> Option<Vec<PyQirOperand>> {
        Some(
            llvm_ir::instruction::Call::try_from(self.instr.clone())
                .ok()?
                .arguments
                .iter()
                .map(|o| PyQirOperand {
                    op: o.0.clone(),
                    types: self.types.clone(),
                })
                .collect(),
        )
    }

    #[getter]
    fn get_is_qis_call(&self) -> bool {
        llvm_ir::instruction::Call::try_from(self.instr.clone()).map_or(false, |c| c.is_qis())
    }

    #[getter]
    fn get_is_qrt_call(&self) -> bool {
        llvm_ir::instruction::Call::try_from(self.instr.clone()).map_or(false, |c| c.is_qrt())
    }

    #[getter]
    fn get_is_qir_call(&self) -> bool {
        llvm_ir::instruction::Call::try_from(self.instr.clone()).map_or(false, |c| c.is_qir())
    }

    #[getter]
    fn get_has_output(&self) -> bool {
        match self.instr.try_get_result() {
            Some(_) => true,
            None => false,
        }
    }

    #[getter]
    fn get_output_name(&self) -> Option<String> {
        Some(self.instr.try_get_result()?.get_string())
    }
}

#[pymethods]
impl PyQirTerminator {
    #[getter]
    fn get_is_ret(&self) -> bool {
        matches!(self.term, llvm_ir::Terminator::Ret(_))
    }

    #[getter]
    fn get_ret_operand(&self) -> Option<PyQirOperand> {
        match_contents!(
            &self.term,
            llvm_ir::Terminator::Ret(llvm_ir::terminator::Ret {
                return_operand,
                debugloc: _,
            }),
            PyQirOperand {
                op: return_operand.as_ref()?.clone(),
                types: self.types.clone(),
            }
        )
    }

    #[getter]
    fn get_is_br(&self) -> bool {
        matches!(self.term, llvm_ir::Terminator::Br(_))
    }

    #[getter]
    fn get_br_dest(&self) -> Option<String> {
        match_contents!(
            &self.term,
            llvm_ir::Terminator::Br(llvm_ir::terminator::Br { dest, debugloc: _ }),
            dest.get_string()
        )
    }

    #[getter]
    fn get_is_condbr(&self) -> bool {
        matches!(self.term, llvm_ir::Terminator::CondBr(_))
    }

    #[getter]
    fn get_condbr_condition(&self) -> Option<PyQirOperand> {
        match_contents!(
            &self.term,
            llvm_ir::Terminator::CondBr(llvm_ir::terminator::CondBr {
                condition,
                true_dest: _,
                false_dest: _,
                debugloc: _,
            }),
            PyQirOperand {
                op: condition.clone(),
                types: self.types.clone(),
            }
        )
    }

    #[getter]
    fn get_condbr_true_dest(&self) -> Option<String> {
        match_contents!(
            &self.term,
            llvm_ir::Terminator::CondBr(llvm_ir::terminator::CondBr {
                condition: _,
                true_dest,
                false_dest: _,
                debugloc: _,
            }),
            true_dest.get_string()
        )
    }

    #[getter]
    fn get_condbr_false_dest(&self) -> Option<String> {
        match_contents!(
            &self.term,
            llvm_ir::Terminator::CondBr(llvm_ir::terminator::CondBr {
                condition: _,
                true_dest: _,
                false_dest,
                debugloc: _,
            }),
            false_dest.get_string()
        )
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
impl PyQirOperand {
    #[getter]
    fn get_is_local(&self) -> bool {
        matches!(self.op, llvm_ir::Operand::LocalOperand { name: _, ty: _ })
    }

    #[getter]
    fn get_local_name(&self) -> Option<String> {
        match_contents!(
            &self.op,
            llvm_ir::Operand::LocalOperand { name, ty: _ },
            name.get_string()
        )
    }

    #[getter]
    fn get_local_type(&self) -> Option<PyQirType> {
        match_contents!(
            &self.op,
            llvm_ir::Operand::LocalOperand { name: _, ty },
            PyQirType {
                typeref: ty.clone(),
            }
        )
    }

    #[getter]
    fn get_is_constant(&self) -> bool {
        matches!(self.op, llvm_ir::Operand::ConstantOperand(_))
    }

    #[getter]
    fn get_constant(&self) -> Option<PyQirConstant> {
        match_contents!(
            &self.op,
            llvm_ir::Operand::ConstantOperand(cref),
            PyQirConstant {
                constantref: cref.clone(),
                types: self.types.clone(),
            }
        )
    }
}

#[pymethods]
impl PyQirConstant {
    #[getter]
    fn get_is_int(&self) -> bool {
        matches!(
            self.constantref.as_ref(),
            llvm_ir::Constant::Int { bits: _, value: _ }
        )
    }

    #[getter]
    fn get_int_value(&self) -> Option<i64> {
        match_contents!(
            self.constantref.as_ref(),
            llvm_ir::Constant::Int { bits: _, value },
            value.clone() as i64
        )
    }

    #[getter]
    fn get_int_width(&self) -> Option<u32> {
        match_contents!(
            &self.constantref.as_ref(),
            llvm_ir::Constant::Int { bits, value: _ },
            bits.clone()
        )
    }

    #[getter]
    fn get_is_float(&self) -> bool {
        matches!(self.constantref.as_ref(), llvm_ir::Constant::Float(_))
    }

    #[getter]
    fn get_float_double_value(&self) -> Option<f64> {
        match_contents!(
            &self.constantref.as_ref(),
            llvm_ir::Constant::Float(llvm_ir::constant::Float::Double(d)),
            d.clone()
        )
    }

    #[getter]
    fn get_is_null(&self) -> bool {
        matches!(self.constantref.as_ref(), llvm_ir::Constant::Null(_))
    }

    #[getter]
    fn get_is_aggregate_zero(&self) -> bool {
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
        matches!(
            self.constantref.as_ref(),
            llvm_ir::Constant::GlobalReference { name: _, ty: _ }
        )
    }

    #[getter]
    fn get_type(&self) -> PyQirType {
        PyQirType {
            typeref: self.constantref.get_type(&self.types),
        }
    }

    #[getter]
    fn get_is_qubit(&self) -> bool {
        self.get_type().get_is_qubit()
    }

    #[getter]
    fn get_qubit_static_id(&self) -> Option<u64> {
        self.constantref.qubit_id()
    }

    #[getter]
    fn get_is_result(&self) -> bool {
        self.get_type().get_is_result()
    }

    #[getter]
    fn get_result_static_id(&self) -> Option<u64> {
        self.constantref.result_id()
    }
}

#[pymethods]
impl PyQirType {
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
    fn get_integer_width(&self) -> Option<u32> {
        match_contents!(
            self.typeref.as_ref(),
            llvm_ir::Type::IntegerType { bits },
            bits.clone()
        )
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
    fn get_pointer_type(&self) -> Option<PyQirType> {
        match_contents!(
            self.typeref.as_ref(),
            llvm_ir::Type::PointerType {
                pointee_type,
                addr_space: _
            },
            PyQirType {
                typeref: pointee_type.clone()
            }
        )
    }

    #[getter]
    fn get_pointer_addrspace(&self) -> Option<u32> {
        match_contents!(
            self.typeref.as_ref(),
            llvm_ir::Type::PointerType {
                pointee_type: _,
                addr_space
            },
            addr_space.clone()
        )
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
    fn get_array_element_type(&self) -> Option<PyQirType> {
        match_contents!(
            self.typeref.as_ref(),
            llvm_ir::Type::ArrayType {
                element_type,
                num_elements: _,
            },
            PyQirType {
                typeref: element_type.clone()
            }
        )
    }

    #[getter]
    fn get_array_num_elements(&self) -> Option<usize> {
        match_contents!(
            self.typeref.as_ref(),
            llvm_ir::Type::ArrayType {
                element_type: _,
                num_elements,
            },
            num_elements.clone()
        )
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
    fn get_struct_element_types(&self) -> Option<Vec<PyQirType>> {
        match_contents!(
            self.typeref.as_ref(),
            llvm_ir::Type::StructType {
                element_types,
                is_packed: _
            },
            element_types
                .iter()
                .map(|t| PyQirType { typeref: t.clone() })
                .collect()
        )
    }

    #[getter]
    fn get_is_named_struct(&self) -> bool {
        matches!(
            self.typeref.as_ref(),
            llvm_ir::Type::NamedStructType { name: _ }
        )
    }

    #[getter]
    fn get_named_struct_name(&self) -> Option<String> {
        match_contents!(
            self.typeref.as_ref(),
            llvm_ir::Type::NamedStructType { name },
            name.clone()
        )
    }

    #[getter]
    fn get_is_qubit(&self) -> bool {
        self.typeref.is_qubit()
    }

    #[getter]
    fn get_is_result(&self) -> bool {
        self.typeref.is_result()
    }
}
