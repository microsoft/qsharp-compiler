// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use inkwell::memory_buffer::MemoryBuffer;
use inkwell::module::Module;
use inkwell::values::FunctionValue;

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

pub(crate) fn load_module_from_bitcode_file<'ctx>(
    context: &'ctx inkwell::context::Context,
    name: &'ctx str,
) -> Module<'ctx> {
    let module_contents = include_bytes!("module.bc");
    let buffer = MemoryBuffer::create_from_memory_range_copy(module_contents, name);
    let module = Module::parse_bitcode_from_buffer(&buffer, context).unwrap();
    module
}
