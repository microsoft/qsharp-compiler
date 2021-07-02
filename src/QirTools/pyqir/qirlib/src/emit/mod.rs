use crate::interop::Register;
use crate::interop::Instruction;
use inkwell::context::Context;
use inkwell::values::BasicValueEnum;

use self::intrinsics::Intrinsics;
use self::runtime_library::RuntimeLibrary;
use self::types::Types;

use super::interop::SemanticModel;
use std::collections::BTreeMap;
use std::path::Path;

pub mod constants;
mod intrinsics;
mod qir;
mod runtime_library;
pub mod types;

pub struct Emitter {}
impl Emitter {
    pub fn write(model: &SemanticModel, file_name: &str) {
        let ctx = Context::create();
        let module_ctx = ModuleContext::new(&ctx, model.name.as_str());
        let emitter_ctx = EmitterContext::new(&ctx, module_ctx);

        let entrypoint = qir::get_entry_function(&emitter_ctx);
        let entry = emitter_ctx.context.append_basic_block(entrypoint, "entry");
        emitter_ctx.module_ctx.builder.position_at_end(entry);

        let qubits = Emitter::write_qubits(&model, &emitter_ctx);

        let registers = Emitter::write_registers(&model, &emitter_ctx);

        let _ = Emitter::write_instructions(&model, &emitter_ctx, &qubits);

        let output = registers.get("results").unwrap();
        emitter_ctx.module_ctx.builder.build_return(Some(output));

        emitter_ctx.emit_ir(file_name);
    }

    fn write_qubits<'ctx>(
        model: &SemanticModel,
        context: &EmitterContext<'ctx>,
    ) -> BTreeMap<String, BasicValueEnum<'ctx>> {
        let mut qubits = BTreeMap::new();
        for reg in model.qubits.iter() {
            match reg {
                Register::Quantum { name, index } => {
                    let indexed_name = format!("{}{}", &name[..], index);
                    let value = qir::emit_allocate_qubit(&context, indexed_name.as_str());
                    qubits.insert(indexed_name, value);
                }
                _ => panic!("qubits shouldn't container classical registers"),
            }
        }
        qubits
    }

    fn write_registers<'ctx>(
        model: &SemanticModel,
        context: &EmitterContext<'ctx>,
    ) -> BTreeMap<String, BasicValueEnum<'ctx>> {
        let mut registers = BTreeMap::new();
        let number_of_registers = model.registers.len() as u64;
        if number_of_registers > 0 {
            let results =
                qir::array1d::emit_array_allocate1d(&context, 8, number_of_registers, "results");
            registers.insert(String::from("results"), results);
            let mut sub_results = vec![];
            let mut index = 0;
            for reg in model.registers.iter() {
                match reg {
                    Register::Classical { name, size } => {
                        let sub_result = qir::array1d::emit_array_1d(context, name, size.clone());
                        sub_results.push(sub_result);
                        registers.insert(name.to_owned(), sub_result);
                    }
                    _ => panic!("registers shouldn't container qubit registers"),
                }
                index += 1;
            }
            qir::array1d::set_elements(&context, &results, sub_results, "results");
            registers
        } else {
            let results = qir::array1d::emit_empty_result_array_allocate1d(&context, "results");
            registers.insert(String::from("results"), results);
            registers
        }
    }

    fn write_instructions<'ctx>(
        model: &SemanticModel,
        context: &EmitterContext<'ctx>,
        qubits: &BTreeMap<String, BasicValueEnum<'ctx>>,
    ) {
        for inst in model.instructions.iter() {
            qir::instructions::emit(context, inst, qubits);
        }
    }
}

pub struct ModuleContext<'ctx> {
    pub context: &'ctx inkwell::context::Context,
    pub module: inkwell::module::Module<'ctx>,
    pub builder: inkwell::builder::Builder<'ctx>,
}


impl<'ctx> ModuleContext<'ctx> {
    pub fn new(context: &'ctx Context, name: &'ctx str) -> Self {
        let builder = context.create_builder();
        ModuleContext {
            context: context,
            module: qir::load_module_from_bitcode_file(&context, name),
            builder: builder,
        }
    }
}

pub struct EmitterContext<'ctx> {
    pub(crate) module_ctx: ModuleContext<'ctx>,
    pub(crate) types: Types<'ctx>,
    pub(crate) context: &'ctx inkwell::context::Context,
    pub(crate) runtime_library: RuntimeLibrary<'ctx>,
    pub(crate) intrinsics: Intrinsics<'ctx>,
}

impl<'ctx> EmitterContext<'ctx> {
    pub fn new(context: &'ctx Context, module: ModuleContext<'ctx>) -> Self {
        let types = Types::new(&context, &module.module);
        let runtime_library = RuntimeLibrary::new(&module.module);
        let intrinsics = Intrinsics::new(&module.module);
        EmitterContext {
            module_ctx: module,
            types: types,
            context: context,
            runtime_library: runtime_library,
            intrinsics: intrinsics,
        }
    }

    pub fn add_boilerplate(&self) {
        let void_type = self.context.void_type();
        let fn_type = void_type.fn_type(&[], false);
        let fn_val = self.module_ctx.module.add_function("my_fn", fn_type, None);
        let basic_block = self.context.append_basic_block(fn_val, "entry");
        self.module_ctx.builder.position_at_end(basic_block);
        self.module_ctx.builder.build_return(None);
    }

    pub fn emit_bitcode(&self, file_path: &str) {
        let bitcode_path = Path::new(file_path);
        self.module_ctx.module.write_bitcode_to_path(&bitcode_path);
    }

    pub fn emit_ir(&self, file_path: &str) {
        let ir_path = Path::new(file_path);
        if let Err(_) = self.module_ctx.module.print_to_file(ir_path) {
            todo!()
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn bell_measure() {
        let name = String::from("Bell circuit");
        let mut model = SemanticModel::new(name);
        model.add_reg(Register::Quantum {
            name: String::from("qr"),
            index: 0,
        });
        model.add_reg(Register::Quantum {
            name: String::from("qr"),
            index: 1,
        });
        model.add_reg(Register::Classical {
            name: String::from("qc"),
            size: 2,
        });
        model.add_inst(Instruction::H(String::from("qr0")));
        model.add_inst(Instruction::Cx {
            control: String::from("qr0"),
            target: String::from("qr1"),
        });
        model.add_inst(Instruction::M {
            qubit: String::from("qr0"),
            target: String::from("qc0"),
        });
        model.add_inst(Instruction::M {
            qubit: String::from("qr1"),
            target: String::from("qc1"),
        });
        Emitter::write(&model, "bell_measure.ll");
    }

    #[test]
    fn bell_no_measure() {
        let name = String::from("Bell circuit");
        let mut model = SemanticModel::new(name);
        model.add_reg(Register::Quantum {
            name: String::from("qr"),
            index: 0,
        });
        model.add_reg(Register::Quantum {
            name: String::from("qr"),
            index: 1,
        });
        model.add_reg(Register::Classical {
            name: String::from("qc"),
            size: 2,
        });
        model.add_inst(Instruction::H(String::from("qr0")));
        model.add_inst(Instruction::Cx {
            control: String::from("qr0"),
            target: String::from("qr1"),
        });
        Emitter::write(&model, "bell_no_measure.ll");
    }
}
