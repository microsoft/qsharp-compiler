use inkwell::memory_buffer::MemoryBuffer;
use inkwell::module::Linkage;
use inkwell::module::Module;
use inkwell::types::BasicType;
use inkwell::types::BasicTypeEnum;
use inkwell::types::StructType;
use inkwell::values::GlobalValue;
use inkwell::values::{BasicValue, BasicValueEnum, FunctionValue};
use inkwell::AddressSpace;

use inkwell::context::Context;

use super::EmitterContext;

pub mod array1d;
pub mod instructions;

pub(crate) fn get_entry_function<'ctx>(emitter_ctx: &'ctx EmitterContext) -> FunctionValue<'ctx> {
    let ns = "QuantumApplication";
    let method = "Run";
    let entrypoint_name = format!("{}__{}__body", ns, method);
    let entrypoint = emitter_ctx
        .module_ctx
        .module
        .get_function(&entrypoint_name)
        .unwrap();

    while let Some(bb) = entrypoint.get_last_basic_block() {
        unsafe {
            bb.delete().unwrap();
        }
    }
    entrypoint
}

pub(crate) fn create_named_opaque_struct<'ctx>(
    context: &'ctx Context,
    name: &str,
    field_types: &[BasicTypeEnum],
) -> StructType<'ctx> {
    let s = context.opaque_struct_type(name);
    s.set_body(field_types, false);
    s
}

pub(crate) fn load_module_from_bitcode_file<'ctx>(
    context: &'ctx Context,
    name: &'ctx str,
) -> Module<'ctx> {
    let module_contents = include_bytes!("module.bc");
    let buffer = MemoryBuffer::create_from_memory_range_copy(module_contents, name);
    let module = Module::parse_bitcode_from_buffer(&buffer, context).unwrap();
    module
}

pub(crate) fn add_global<'ctx, T: BasicType<'ctx>>(
    emitter_ctx: &EmitterContext<'ctx>,
    type_: T,
    name: &str,
    value: &dyn BasicValue<'ctx>,
) -> GlobalValue<'ctx> {
    let x = emitter_ctx
        .module_ctx
        .module
        .add_global(type_, Some(AddressSpace::Const), name);
    x.set_constant(true);
    x.set_linkage(Linkage::Internal);

    x.set_initializer(value);
    x
}

pub(crate) fn emit_allocate_qubit<'ctx>(
    emitter_ctx: &EmitterContext<'ctx>,
    result_name: &str,
) -> BasicValueEnum<'ctx> {
    let args = &[];
    let lhs = emitter_ctx
        .module_ctx
        .builder
        .build_call(emitter_ctx.runtime_library.QubitAllocate, args, result_name)
        .try_as_basic_value();
    lhs.left().unwrap()
}

pub(crate) fn emit_release_qubit<'ctx>(
    emitter_ctx: &EmitterContext<'ctx>,
    qubit: &BasicValueEnum<'ctx>,
) {
    let args = [qubit.as_basic_value_enum()];
    let lhs = emitter_ctx
        .module_ctx
        .builder
        .build_call(emitter_ctx.runtime_library.QubitRelease, &args, "")
        .try_as_basic_value();
    lhs.right().unwrap();
}