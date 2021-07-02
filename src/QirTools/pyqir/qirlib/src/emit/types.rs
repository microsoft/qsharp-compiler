use inkwell::context::Context;
use inkwell::module::Module;
use inkwell::types::FloatType;
use inkwell::types::IntType;
use inkwell::types::PointerType;
use inkwell::types::StructType;
use inkwell::AddressSpace;

pub const Int: &str = "Int";
pub const Double: &str = "Double";
pub const Bool: &str = "Bool";
pub const Pauli: &str = "Pauli";

pub const Callable: &str = "Callable";
pub const Result: &str = "Result";
pub const Qubit: &str = "Qubit";
pub const Range: &str = "Range";
pub const BigInt: &str = "BigInt";
pub const String: &str = "String";
pub const Array: &str = "Array";
pub const Tuple: &str = "Tuple";

pub struct Types<'ctx> {
    pub(crate) int: IntType<'ctx>,
    pub(crate) double: FloatType<'ctx>,
    pub(crate) bool: IntType<'ctx>,
    pub(crate) pauli: IntType<'ctx>,
    pub(crate) range: StructType<'ctx>,
    pub(crate) result: PointerType<'ctx>,
    pub(crate) qubit: PointerType<'ctx>,
    pub(crate) string: PointerType<'ctx>,
    //pub(crate) big_int: PointerType<'ctx>,
    pub(crate) tuple: PointerType<'ctx>,
    pub(crate) array: PointerType<'ctx>,
    pub(crate) callable: PointerType<'ctx>,
}

impl<'ctx> Types<'ctx> {
    pub fn new(context: &'ctx Context, module: &Module<'ctx>) -> Self {
        Types {
            int: context.i64_type(),
            double: context.f64_type(),
            bool: context.bool_type(),
            pauli: context.custom_width_int_type(2),

            range: Types::get_struct(module, Range),

            result: Types::get_struct_pointer(module, Result),
            qubit: Types::get_struct_pointer(module, Qubit),
            string: Types::get_struct_pointer(module, String),
            // todo: big_int isn't defined in the current template .ll
            //big_int: Types::get_struct_pointer(module, BigInt),
            tuple: Types::get_struct_pointer(module, Tuple),
            array: Types::get_struct_pointer(module, Array),
            callable: Types::get_struct_pointer(module, Callable),
        }
    }

    fn get_struct(module: &Module<'ctx>, name: &str) -> StructType<'ctx> {
        if let Some(defined_struct) = module.get_struct_type(name) {
            return defined_struct;
        }
        panic!("{} was not defined in the module", name);
    }

    fn get_struct_pointer(module: &Module<'ctx>, name: &str) -> PointerType<'ctx> {
        if let Some(defined_struct) = module.get_struct_type(name) {
            return defined_struct.ptr_type(AddressSpace::Generic);
        }
        panic!("{} struct was not defined in the module", name);
    }
}

#[cfg(test)]
mod tests {
    use inkwell::context::Context;

    use crate::emit::ModuleContext;

    use super::*;

    #[test]
    fn types_can_be_loaded() {
        let ctx = Context::create();
        let name = "temp";
        let module_ctx = ModuleContext::new(&ctx, name);
        let _ = Types::new(&module_ctx.context, &module_ctx.module);
    }
}
