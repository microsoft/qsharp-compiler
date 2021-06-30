use either::Either;
use inkwell::memory_buffer::MemoryBuffer;
use inkwell::module::Linkage;
use inkwell::module::Module;
use inkwell::types::BasicType;
use inkwell::types::BasicTypeEnum;
use inkwell::types::StructType;
use inkwell::values::{BasicValue, BasicValueEnum, FunctionValue};
use inkwell::values::{GlobalValue, InstructionValue};
use inkwell::AddressSpace;

use crate::interop::Register;
use inkwell::context::Context;

use self::runtime_library::RuntimeLibrary;
use self::types::Types;

use super::interop::{Instruction, SemanticModel};
use std::path::Path;

pub mod constants;
mod intrinsics;
mod runtime_library;
pub mod types;

fn create_named_opaque_struct<'ctx>(
    context: &'ctx Context,
    name: &str,
    field_types: &[BasicTypeEnum],
) -> StructType<'ctx> {
    let s = context.opaque_struct_type(name);
    s.set_body(field_types, false);
    s
}

pub struct ModuleContext<'ctx> {
    pub context: &'ctx inkwell::context::Context,
    pub module: inkwell::module::Module<'ctx>,
    pub builder: inkwell::builder::Builder<'ctx>,
}

pub struct EmitterContext<'ctx> {
    pub(crate) module_ctx: ModuleContext<'ctx>,
    pub(crate) types: Types<'ctx>,
    pub(crate) context: &'ctx inkwell::context::Context,
    pub(crate) runtime_library: RuntimeLibrary<'ctx>,
}

pub struct Emitter {}
impl Emitter {
    pub fn write(model: &SemanticModel, file_name: &str) {
        let ctx = Context::create();
        let module_ctx = ModuleContext::new(&ctx, model.name.as_str());
        let emitter_ctx = EmitterContext::new(&ctx, module_ctx);
        
        //emitter_ctx.add_boilerplate();
        let entrypoint = Emitter::get_entry_function(&emitter_ctx);
        let entry = emitter_ctx.context.append_basic_block(entrypoint, "entry");
        emitter_ctx.module_ctx.builder.position_at_end(entry);

        Emitter::write_registers(&model, &emitter_ctx);

        emitter_ctx.emit_ir(file_name);
    }

    fn get_entry_function<'ctx>(emitter_ctx: &'ctx EmitterContext) -> FunctionValue<'ctx> {
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

    fn write_registers(model: &SemanticModel, context: &EmitterContext) {
        for reg in model.registers.iter() {
            match reg {
                Register::Classical { name, size } => {
                    let _ = emit_array_allocate1d(&context, size.clone(), 1, &name[..]);
                }
                Register::Quantum { name, index } => {
                    let indexed_name = format!("{}_{}", &name[..], index);
                    let _ = emit_allocate_qubit(&context,  indexed_name.as_str());
                }
            }
        }
    }

    fn write_instructions(model: &SemanticModel, context: &EmitterContext) {
        for inst in model.instructions.iter() {
            match inst {
                Instruction::M { qubit, target } => {
                    todo!("write measure")
                }
                _ => {
                    todo!("write intrinsic")
                }
            }
        }
    }
}

fn emit_array_allocate1d<'ctx>(
    emitter_ctx: &EmitterContext<'ctx>,
    length: u64,
    bits: u64,
    result_name: &str,
) -> BasicValueEnum<'ctx> {
    let args = &[
        emitter_ctx
            .context
            .i32_type()
            .const_int(length, false)
            .as_basic_value_enum(),
        emitter_ctx
            .types
            .int
            .const_int(bits, false)
            .as_basic_value_enum(),
    ];
    let lhs = emitter_ctx
        .module_ctx
        .builder
        .build_call(emitter_ctx.runtime_library.ArrayCreate1d, args, result_name)
        .try_as_basic_value();
    lhs.left().unwrap()
}

fn emit_allocate_qubit<'ctx>(
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

impl<'ctx> ModuleContext<'ctx> {
    pub fn new(context: &'ctx Context, name: &'ctx str) -> Self {
        let builder = context.create_builder();
        ModuleContext {
            context: context,
            module: EmitterContext::load_module_from_bitcode_file(&context, name),
            builder: builder,
        }
    }
}

impl<'ctx> EmitterContext<'ctx> {
    pub fn new(context: &'ctx Context, module: ModuleContext<'ctx>) -> Self {
        let types = Types::new(&context, &module.module);
        let runtime_library = RuntimeLibrary::new(&module.module);
        EmitterContext {
            module_ctx: module,
            types: types,
            context: context,
            runtime_library: runtime_library,
        }
    }

    pub fn load_module_from_bitcode_file(context: &'ctx Context, name: &'ctx str) -> Module<'ctx> {
        let module_contents = include_bytes!("module.bc");
        let buffer = MemoryBuffer::create_from_memory_range_copy(module_contents, name);
        let module = Module::parse_bitcode_from_buffer(&buffer, context).unwrap();
        module
    }

    pub fn add_global<T: BasicType<'ctx>>(
        &self,
        type_: T,
        name: &str,
        value: &dyn BasicValue<'ctx>,
    ) -> GlobalValue<'ctx> {
        let x = self
            .module_ctx
            .module
            .add_global(type_, Some(AddressSpace::Const), name);
        x.set_constant(true);
        x.set_linkage(Linkage::Internal);

        x.set_initializer(value);
        x
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
    fn smoke() {
        let name = String::from("name");
        let model = SemanticModel::new(name);
        Emitter::write(&model, "file_name.ll");
    }
    #[test]
    fn h_adjusts_context() {
        let name = String::from("name");
        let mut model = SemanticModel::new(name);
        let inst = Instruction::H(String::from("input_0"));
        model.add_inst(inst);

        assert_eq!(model.instructions.len(), 1);
        match model.instructions.into_iter().next().unwrap() {
            Instruction::H(name) => {
                assert_eq!("input_0", name);
            }
            _ => panic!(),
        }
    }
}
