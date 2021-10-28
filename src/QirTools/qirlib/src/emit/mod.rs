// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use self::constants::Constants;
use self::intrinsics::Intrinsics;
use self::runtime_library::RuntimeLibrary;
use self::types::Types;
use inkwell::module::Module;
use inkwell::values::BasicValueEnum;

use super::interop::SemanticModel;
use std::collections::HashMap;
use std::path::Path;

pub mod constants;
pub mod intrinsics;
pub mod qir;
mod runtime_library;
pub mod types;

pub fn populate_context<'a>(
    ctx: &'a inkwell::context::Context,
    model: &'a SemanticModel,
) -> Result<Context<'a>, String> {
    let context_type = ContextType::Template(&model.name);
    match Context::new(&ctx, context_type) {
        Err(err) => {
            let message = err.to_string();
            return Err(message);
        }
        Ok(context) => {
            build_entry_function(&context, model)?;
            Ok(context)
        }
    }
}

fn build_entry_function(context: &Context<'_>, model: &SemanticModel) -> Result<(), String> {
    let entrypoint = qir::get_entry_function(context);

    let entry = context.context.append_basic_block(entrypoint, "entry");
    context.builder.position_at_end(entry);

    let qubits = write_qubits(&model, context);

    let registers = write_registers(&model, context);

    write_instructions(&model, context, &qubits, &registers);

    free_qubits(context, &qubits);

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

pub struct Context<'ctx> {
    pub(crate) context: &'ctx inkwell::context::Context,
    pub(crate) module: inkwell::module::Module<'ctx>,
    pub(crate) builder: inkwell::builder::Builder<'ctx>,
    pub(crate) types: Types<'ctx>,
    pub(crate) runtime_library: RuntimeLibrary<'ctx>,
    pub(crate) intrinsics: Intrinsics<'ctx>,
    pub(crate) constants: Constants<'ctx>,
}

pub enum ContextType<'ctx> {
    Template(&'ctx String),
    File(&'ctx String),
}

impl<'ctx> Context<'ctx> {
    pub fn new(
        context: &'ctx inkwell::context::Context,
        context_type: ContextType<'ctx>,
    ) -> Result<Self, String> {
        let builder = context.create_builder();
        let module = Context::load_module(context, context_type)?;
        let types = Types::new(&context, &module);
        let runtime_library = RuntimeLibrary::new(&module);
        let intrinsics = Intrinsics::new(&module);
        let constants = Constants::new(&module, &types);
        Ok(Context {
            builder,
            module,
            types,
            context,
            runtime_library,
            intrinsics,
            constants,
        })
    }
    fn load_module(
        context: &'ctx inkwell::context::Context,
        context_type: ContextType<'ctx>,
    ) -> Result<Module<'ctx>, String> {
        let module = match context_type {
            ContextType::Template(name) => {
                qir::load_module_from_bitcode_template(&context, &name[..])?
            }
            ContextType::File(file_name) => {
                let file_path = Path::new(&file_name[..]);
                let ext = file_path.extension().and_then(std::ffi::OsStr::to_str);
                let module = match ext {
                    Some("ll") => qir::load_module_from_ir_file(file_path, context)?,
                    Some("bc") => qir::load_module_from_bitcode_file(file_path, context)?,
                    _ => panic!("Unsupported module exetension {:?}", ext),
                };
                module
            }
        };
        Ok(module)
    }
    pub fn emit_bitcode(&self, file_path: &str) {
        let bitcode_path = Path::new(file_path);
        self.module.write_bitcode_to_path(&bitcode_path);
    }

    pub fn emit_ir(&self, file_path: &str) -> Result<(), String> {
        let ir_path = Path::new(file_path);
        if let Err(llvmstr) = self.module.print_to_file(ir_path) {
            return Err(llvmstr.to_string());
        }
        Ok(())
    }

    pub fn get_ir_string(&self) -> String {
        let ir = self.module.print_to_string();
        let result = ir.to_string();
        result
    }

    pub fn get_bitcode_base64_string(&self) -> String {
        let buffer = self.module.write_bitcode_to_memory();
        let bytes = buffer.as_slice();
        let result = base64::encode(bytes);
        result
    }
}

#[cfg(test)]
mod tests {
    use crate::emit::{Context, ContextType};
    use std::fs::File;
    use std::io::prelude::*;

    use tempfile::tempdir;

    #[test]
    fn emitted_bitcode_files_are_identical_to_base64_encoded() {
        let dir = tempdir().expect("");
        let tmp_path = dir.into_path();
        let name = String::from("test");
        let file_path = tmp_path.join(format!("{}.bc", name));
        let file_path_string = file_path.display().to_string();

        let ctx = inkwell::context::Context::create();
        let name = String::from("temp");
        let context = Context::new(&ctx, ContextType::Template(&name)).unwrap();
        context.emit_bitcode(file_path_string.as_str());
        let mut emitted_bitcode_file =
            File::open(file_path_string.as_str()).expect("Could not open emitted bitcode file");
        let mut buffer = vec![];

        emitted_bitcode_file
            .read_to_end(&mut buffer)
            .expect("Could not read emitted bitcode file");
        let emitted_bitcode_bytes = buffer.as_slice();

        let b64_bitcode = context.get_bitcode_base64_string();
        let decoded = base64::decode(b64_bitcode).expect("could not decode base64 encoded module");
        let decoded_bitcode_bytes = decoded.as_slice();

        assert_eq!(emitted_bitcode_bytes, decoded_bitcode_bytes);
    }
}
