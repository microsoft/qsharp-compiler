// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use std::path::Path;

use inkwell::memory_buffer::MemoryBuffer;
use inkwell::module::Module;
use inkwell::values::BasicValue;
use inkwell::values::FunctionValue;
use inkwell::AddressSpace;

use super::Context;

pub mod array1d;
pub mod basic_values;
pub mod calls;
pub mod instructions;
pub mod qubits;

pub(crate) fn get_entry_function<'ctx>(context: &Context<'ctx>) -> FunctionValue<'ctx> {
    let ns = "QuantumApplication";
    let method = "Run";
    let entrypoint_name = format!("{}__{}__body", ns, method);
    let entrypoint = context.module.get_function(&entrypoint_name).unwrap();

    while let Some(basic_block) = entrypoint.get_last_basic_block() {
        unsafe {
            basic_block.delete().unwrap();
        }
    }
    entrypoint
}

pub(crate) fn remove_quantumapplication_run<'ctx>(context: &Context<'ctx>) -> FunctionValue<'ctx> {
    let ns = "QuantumApplication";
    let method = "Run";
    let entrypoint_name = format!("{}__{}", ns, method);
    let entrypoint = context.module.get_function(&entrypoint_name).unwrap();
    while let Some(basic_block) = entrypoint.get_last_basic_block() {
        unsafe {
            basic_block.delete().unwrap();
        }
    }
    entrypoint
}
pub(crate) fn remove_quantumapplication_run_interop<'ctx>(
    context: &Context<'ctx>,
) -> FunctionValue<'ctx> {
    let ns = "QuantumApplication";
    let method = "Run";
    let entrypoint_name = format!("{}__{}__Interop", ns, method);
    let entrypoint = context.module.get_function(&entrypoint_name).unwrap();
    while let Some(basic_block) = entrypoint.get_last_basic_block() {
        unsafe {
            basic_block.delete().unwrap();
        }
    }
    let entry = context.context.append_basic_block(entrypoint, "entry");
    context.builder.position_at_end(entry);

    let v = entrypoint
        .get_type()
        .ptr_type(AddressSpace::Generic)
        .const_null()
        .as_basic_value_enum();
    context.builder.build_return(Some(&v));
    entrypoint
}

pub(crate) fn load_module_from_bitcode_template<'ctx>(
    context: &'ctx inkwell::context::Context,
    name: &'ctx str,
) -> Result<Module<'ctx>, String> {
    let module_contents = include_bytes!("module.bc");
    let buffer = MemoryBuffer::create_from_memory_range_copy(module_contents, name);
    match Module::parse_bitcode_from_buffer(&buffer, context) {
        Err(err) => {
            let message = err.to_string();
            return Err(message);
        }
        Ok(module) => Ok(module),
    }
}

pub(crate) fn load_module_from_bitcode_file<'ctx, P: AsRef<Path>>(
    path: P,
    context: &'ctx inkwell::context::Context,
) -> Result<Module<'ctx>, String> {
    match Module::parse_bitcode_from_path(path, context) {
        Err(err) => {
            let message = err.to_string();
            return Err(message);
        }
        Ok(module) => Ok(module),
    }
}

pub(crate) fn load_module_from_ir_file<'ctx, P: AsRef<Path>>(
    path: P,
    context: &'ctx inkwell::context::Context,
) -> Result<Module<'ctx>, String> {
    let memory_buffer = load_memory_buffer_from_ir_file(path)?;

    match context.create_module_from_ir(memory_buffer) {
        Err(err) => {
            let message = err.to_string();
            return Err(message);
        }
        Ok(module) => Ok(module),
    }
}

fn load_memory_buffer_from_ir_file<'ctx, P: AsRef<Path>>(path: P) -> Result<MemoryBuffer, String> {
    match MemoryBuffer::create_from_file(path.as_ref()) {
        Err(err) => {
            let message = err.to_string();
            return Err(message);
        }
        Ok(memory_buffer) => Ok(memory_buffer),
    }
}
