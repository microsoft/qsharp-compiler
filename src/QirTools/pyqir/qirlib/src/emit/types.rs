// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use inkwell::context::Context;
use inkwell::module::Module;
use inkwell::types::FloatType;
use inkwell::types::IntType;
use inkwell::types::PointerType;
use inkwell::types::StructType;
use inkwell::AddressSpace;

const INT: &str = "Int";
const DOUBLE: &str = "Double";
const BOOL: &str = "Bool";
const PAULI: &str = "Pauli";
const CALLABLE: &str = "Callable";
const RESULT: &str = "Result";
const QUBIT: &str = "Qubit";
const RANGE: &str = "Range";
const BIG_INT: &str = "BigInt";
const STRING: &str = "String";
const ARRAY: &str = "Array";
const TUPLE: &str = "Tuple";

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

            range: Types::get_struct(module, RANGE),

            result: Types::get_struct_pointer(module, RESULT),
            qubit: Types::get_struct_pointer(module, QUBIT),
            string: Types::get_struct_pointer(module, STRING),
            // todo: big_int isn't defined in the current template .ll
            //big_int: Types::get_struct_pointer(module, BIG_INT),
            tuple: Types::get_struct_pointer(module, TUPLE),
            array: Types::get_struct_pointer(module, ARRAY),
            callable: Types::get_struct_pointer(module, CALLABLE),
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
    use crate::emit::Context;

    use super::*;

    #[test]
    fn types_can_be_loaded() {
        let ctx = inkwell::context::Context::create();
        let name = "temp";
        let context = Context::new(&ctx, name);
        let _ = Types::new(&context.context, &context.module);
    }
}
