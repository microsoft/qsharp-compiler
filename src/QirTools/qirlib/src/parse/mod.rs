// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use std::convert::TryFrom;
use std::num::ParseIntError;

use llvm_ir;

// This module introduces extensions to the existing types exposed by llvm_ir to bring in some
// convenience functions as well as QIR-specific utilities.

pub trait ModuleExt {
    fn get_funcs_by_attr_name(&self, name: &str) -> Vec<&llvm_ir::Function>;
    fn get_entrypoint_funcs(&self) -> Vec<&llvm_ir::Function>;
    fn get_interop_funcs(&self) -> Vec<&llvm_ir::Function>;
}

impl ModuleExt for llvm_ir::Module {
    fn get_funcs_by_attr_name(&self, name: &str) -> Vec<&llvm_ir::Function> {
        self.functions
            .iter()
            .filter(|f| {
                f.function_attributes.contains(
                    &llvm_ir::function::FunctionAttribute::StringAttribute {
                        kind: name.to_string(),
                        value: String::new(),
                    },
                )
            })
            .collect()
    }

    fn get_entrypoint_funcs(&self) -> Vec<&llvm_ir::Function> {
        self.get_funcs_by_attr_name("EntryPoint")
    }

    fn get_interop_funcs(&self) -> Vec<&llvm_ir::Function> {
        self.get_funcs_by_attr_name("InteropFriendly")
    }
}

pub trait FunctionExt {
    fn get_attribute_value(&self, name: &str) -> Option<String>;
    fn get_required_qubits(&self) -> Result<Option<i64>, ParseIntError>;
    fn get_required_results(&self) -> Result<Option<i64>, ParseIntError>;
    fn get_instruction_by_output_name(&self, name: &str) -> Option<&llvm_ir::Instruction>;
}

impl FunctionExt for llvm_ir::Function {
    fn get_attribute_value(&self, name: &str) -> Option<String> {
        for attr in &self.function_attributes {
            match attr {
                llvm_ir::function::FunctionAttribute::StringAttribute { kind, value } => {
                    if kind.to_string().eq(name) {
                        return Some(value.to_string());
                    }
                }
                _ => continue,
            }
        }
        None
    }

    fn get_required_qubits(&self) -> Result<Option<i64>, ParseIntError> {
        match self.get_attribute_value("requiredQubits") {
            Some(s) => Ok(Some(s.parse()?)),
            None => Ok(None),
        }
    }

    fn get_required_results(&self) -> Result<Option<i64>, ParseIntError> {
        match self.get_attribute_value("requiredResults") {
            Some(s) => Ok(Some(s.parse()?)),
            None => Ok(None),
        }
    }

    fn get_instruction_by_output_name(&self, name: &str) -> Option<&llvm_ir::Instruction> {
        for block in &self.basic_blocks {
            for instr in &block.instrs {
                match instr.try_get_result() {
                    Some(resname) => {
                        if resname.get_string().eq(name) {
                            return Some(instr);
                        }
                    }
                    None => continue,
                }
            }
        }
        None
    }
}

pub trait BasicBlockExt {
    fn get_phi_nodes(&self) -> Vec<llvm_ir::instruction::Phi>;
    fn get_phi_pairs_by_source_name(&self, name: &str) -> Vec<(llvm_ir::Name, llvm_ir::Operand)>;
}

impl BasicBlockExt for llvm_ir::BasicBlock {
    fn get_phi_nodes(&self) -> Vec<llvm_ir::instruction::Phi> {
        self.instrs
            .iter()
            .filter_map(|i| llvm_ir::instruction::Phi::try_from(i.clone()).ok())
            .collect()
    }

    fn get_phi_pairs_by_source_name(&self, name: &str) -> Vec<(llvm_ir::Name, llvm_ir::Operand)> {
        self.get_phi_nodes()
            .iter()
            .filter_map(|phi| Some((phi.dest.clone(), phi.get_incoming_value_for_name(name)?)))
            .collect()
    }
}

pub trait IntructionExt {
    fn get_target_operands(&self) -> Vec<llvm_ir::Operand>;
}

impl IntructionExt for llvm_ir::Instruction {
    fn get_target_operands(&self) -> Vec<llvm_ir::Operand> {
        match &self {
            llvm_ir::Instruction::Add(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::Sub(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::Mul(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::UDiv(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::SDiv(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::URem(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::SRem(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::And(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::Or(instr) => vec![instr.operand0.clone(), instr.operand1.clone()],
            llvm_ir::Instruction::Xor(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::Shl(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::LShr(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::AShr(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::FAdd(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::FSub(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::FMul(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::FDiv(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::FRem(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::FNeg(instr) => vec![instr.operand.clone()],
            llvm_ir::Instruction::ICmp(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            llvm_ir::Instruction::FCmp(instr) => {
                vec![instr.operand0.clone(), instr.operand1.clone()]
            }
            _ => vec![],
        }
    }
}

pub trait CallExt {
    fn get_func_name(&self) -> Option<llvm_ir::Name>;
    fn is_qis(&self) -> bool;
    fn is_rt(&self) -> bool;
    fn is_qir(&self) -> bool;
}

impl CallExt for llvm_ir::instruction::Call {
    fn get_func_name(&self) -> Option<llvm_ir::Name> {
        match self.function.clone().right()? {
            llvm_ir::Operand::ConstantOperand(c) => match c.as_ref() {
                llvm_ir::constant::Constant::GlobalReference { name, ty: _ } => Some(name.clone()),
                _ => None,
            },
            _ => None,
        }
    }

    fn is_qis(&self) -> bool {
        self.get_func_name()
            .map_or(false, |n| n.get_string().starts_with("__quantum__qis__"))
    }
    fn is_rt(&self) -> bool {
        self.get_func_name()
            .map_or(false, |n| n.get_string().starts_with("__quantum__rt__"))
    }
    fn is_qir(&self) -> bool {
        self.get_func_name()
            .map_or(false, |n| n.get_string().starts_with("__quantum__qir__"))
    }
}

pub trait PhiExt {
    fn get_incoming_value_for_name(&self, name: &str) -> Option<llvm_ir::Operand>;
}

impl PhiExt for llvm_ir::instruction::Phi {
    fn get_incoming_value_for_name(&self, name: &str) -> Option<llvm_ir::Operand> {
        self.incoming_values.iter().find_map(|(op, block_name)| {
            match block_name.get_string().eq(name) {
                true => Some(op.clone()),
                false => None,
            }
        })
    }
}

pub trait TypeExt {
    fn is_qubit(&self) -> bool;
    fn is_result(&self) -> bool;
}

impl TypeExt for llvm_ir::Type {
    fn is_qubit(&self) -> bool {
        match self {
            llvm_ir::Type::PointerType {
                pointee_type,
                addr_space: _,
            } => pointee_type.as_ref().is_qubit(),
            llvm_ir::Type::NamedStructType { name } => name == "Qubit",
            _ => false,
        }
    }

    fn is_result(&self) -> bool {
        match self {
            llvm_ir::Type::PointerType {
                pointee_type,
                addr_space: _,
            } => pointee_type.as_ref().is_result(),
            llvm_ir::Type::NamedStructType { name } => name == "Result",
            _ => false,
        }
    }
}

pub trait ConstantExt {
    fn qubit_id(&self) -> Option<u64>;
    fn result_id(&self) -> Option<u64>;
}

macro_rules! constant_id {
    ($name:ident, $check_func:path) => {
        fn $name(&self) -> Option<u64> {
            match &self {
                llvm_ir::Constant::Null(t) => {
                    if $check_func(t.as_ref()) {
                        Some(0)
                    } else {
                        None
                    }
                }
                llvm_ir::Constant::IntToPtr(llvm_ir::constant::IntToPtr { operand, to_type }) => {
                    match ($check_func(to_type.as_ref()), operand.as_ref()) {
                        (true, llvm_ir::Constant::Int { bits: 64, value }) => Some(value.clone()),
                        _ => None,
                    }
                }
                _ => None,
            }
        }
    };
}

impl ConstantExt for llvm_ir::Constant {
    constant_id!(qubit_id, TypeExt::is_qubit);
    constant_id!(result_id, TypeExt::is_result);
}

pub trait NameExt {
    fn get_string(&self) -> String;
}

impl NameExt for llvm_ir::Name {
    fn get_string(&self) -> String {
        match &self {
            llvm_ir::Name::Name(n) => n.to_string(),
            llvm_ir::Name::Number(n) => n.to_string(),
        }
    }
}
