// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use self::constants::Constants;
use self::intrinsics::Intrinsics;
use self::runtime_library::RuntimeLibrary;
use self::types::Types;
use inkwell::values::BasicValueEnum;

use super::interop::SemanticModel;
use std::collections::HashMap;
use std::path::Path;

pub mod constants;
mod intrinsics;
pub mod qir;
mod runtime_library;
pub mod types;

pub struct Emitter {}
impl Emitter {
    pub fn write(model: &SemanticModel, file_name: &str) -> Result<(), String> {
        let ctx = inkwell::context::Context::create();
        let context = Context::new(&ctx, model.name.as_str());

        Emitter::build_entry_function(&context, model)?;
        context.emit_ir(file_name);

        Ok(())
    }

    pub fn get_ir_string(model: &SemanticModel) -> Result<String, String> {
        let ctx = inkwell::context::Context::create();
        let context = Context::new(&ctx, model.name.as_str());

        Emitter::build_entry_function(&context, model)?;

        Ok(context.get_ir_string())
    }

    pub fn build_entry_function(
        context: &Context<'_>,
        model: &SemanticModel,
    ) -> Result<(), String> {
        let entrypoint = qir::get_entry_function(context);

        let entry = context.context.append_basic_block(entrypoint, "entry");
        context.builder.position_at_end(entry);

        let qubits = Emitter::write_qubits(&model, context);

        let registers = Emitter::write_registers(&model, context);

        Emitter::write_instructions(&model, context, &qubits, &registers);

        Emitter::free_qubits(context, &qubits);

        let output = registers.get("results").unwrap();
        context.builder.build_return(Some(&output.0));

        if let Err(err) = context.module.verify() {
            let message = err.to_string();
            return Err(message);
        }
        Ok(())
    }

    fn free_qubits<'ctx>(context: &Context<'ctx>, qubits: &HashMap<String, BasicValueEnum<'ctx>>) {
        for (_, value) in qubits.iter() {
            qir::qubits::emit_release(context, value);
        }
    }

    fn write_qubits<'ctx>(
        model: &SemanticModel,
        context: &Context<'ctx>,
    ) -> HashMap<String, BasicValueEnum<'ctx>> {
        let qubits = model
            .qubits
            .iter()
            .map(|reg| {
                let indexed_name = format!("{}{}", &reg.name[..], reg.index);
                let value = qir::qubits::emit_allocate(&context, indexed_name.as_str());
                (indexed_name, value)
            })
            .collect();

        qubits
    }

    fn write_registers<'ctx>(
        model: &SemanticModel,
        context: &Context<'ctx>,
    ) -> HashMap<String, (BasicValueEnum<'ctx>, Option<u64>)> {
        let mut registers = HashMap::new();
        let number_of_registers = model.registers.len() as u64;
        if number_of_registers > 0 {
            let results =
                qir::array1d::emit_array_allocate1d(&context, 8, number_of_registers, "results");
            registers.insert(String::from("results"), (results, None));
            let mut sub_results = vec![];
            for reg in model.registers.iter() {
                let (sub_result, entries) =
                    qir::array1d::emit_array_1d(context, reg.name.as_str(), reg.size.clone());
                sub_results.push(sub_result);
                registers.insert(reg.name.clone(), (sub_result, None));
                for (index, _) in entries {
                    registers.insert(format!("{}{}", reg.name, index), (sub_result, Some(index)));
                }
            }
            qir::array1d::set_elements(&context, &results, sub_results, "results");
            registers
        } else {
            let results = qir::array1d::emit_empty_result_array_allocate1d(&context, "results");
            registers.insert(String::from("results"), (results, None));
            registers
        }
    }

    fn write_instructions<'ctx>(
        model: &SemanticModel,
        context: &Context<'ctx>,
        qubits: &HashMap<String, BasicValueEnum<'ctx>>,
        registers: &HashMap<String, (BasicValueEnum<'ctx>, Option<u64>)>,
    ) {
        for inst in model.instructions.iter() {
            qir::instructions::emit(context, inst, qubits, registers);
        }
    }
}

pub struct Context<'ctx> {
    pub(crate) context: &'ctx inkwell::context::Context,
    pub(crate) module: inkwell::module::Module<'ctx>,
    pub(crate) builder: inkwell::builder::Builder<'ctx>,
    pub(crate) types: Types<'ctx>,
    pub(crate) runtime_library: RuntimeLibrary<'ctx>,
    pub(crate) intrinsics: Intrinsics<'ctx>,
    pub(crate) constants: Constants<'ctx>,
}

impl<'ctx> Context<'ctx> {
    pub fn new(context: &'ctx inkwell::context::Context, name: &'ctx str) -> Self {
        let builder = context.create_builder();

        let module = qir::load_module_from_bitcode_file(&context, name);

        let types = Types::new(&context, &module);
        let runtime_library = RuntimeLibrary::new(&module);
        let intrinsics = Intrinsics::new(&module);
        let constants = Constants::new(&module, &types);
        Context {
            builder,
            module,
            types,
            context,
            runtime_library,
            intrinsics,
            constants,
        }
    }

    pub fn emit_bitcode(&self, file_path: &str) {
        let bitcode_path = Path::new(file_path);
        self.module.write_bitcode_to_path(&bitcode_path);
    }

    pub fn emit_ir(&self, file_path: &str) {
        let ir_path = Path::new(file_path);
        if let Err(_) = self.module.print_to_file(ir_path) {
            todo!()
        }
    }

    pub fn get_ir_string(&self) -> String {
        let ir = self.module.print_to_string();
        let result = ir.to_string();
        result
    }
}
