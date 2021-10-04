// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use inkwell::module::Module;
use inkwell::values::FunctionValue;

pub(crate) struct RuntimeLibrary<'ctx> {
    pub(crate) result_get_zero: FunctionValue<'ctx>,
    pub(crate) result_get_one: FunctionValue<'ctx>,
    pub(crate) result_update_reference_count: FunctionValue<'ctx>,
    pub(crate) result_equal: FunctionValue<'ctx>,
    pub(crate) array_create_1d: FunctionValue<'ctx>,
    pub(crate) array_get_size_1d: FunctionValue<'ctx>,
    pub(crate) array_get_element_ptr_1d: FunctionValue<'ctx>,
    pub(crate) array_update_alias_count: FunctionValue<'ctx>,
    pub(crate) array_update_reference_count: FunctionValue<'ctx>,
    pub(crate) memory_allocate: Option<FunctionValue<'ctx>>,
    pub(crate) message: FunctionValue<'ctx>,
    pub(crate) string_create: FunctionValue<'ctx>,
    pub(crate) string_update_reference_count: FunctionValue<'ctx>,
    pub(crate) string_concatenate: FunctionValue<'ctx>,
    pub(crate) result_to_string: FunctionValue<'ctx>,
    pub(crate) qubit_allocate: FunctionValue<'ctx>,
    pub(crate) qubit_allocate_array: Option<FunctionValue<'ctx>>,
    pub(crate) qubit_release: FunctionValue<'ctx>,
    pub(crate) qubit_release_array: Option<FunctionValue<'ctx>>,
}

impl<'ctx> RuntimeLibrary<'ctx> {
    pub fn new(module: &Module<'ctx>) -> Self {
        RuntimeLibrary {
            result_get_zero: RuntimeLibrary::get_function(module, "result_get_zero")
                .expect("__quantum__rt__result_get_zero function must be defined"),
            result_get_one: RuntimeLibrary::get_function(module, "result_get_one")
                .expect("__quantum__rt__result_get_one function must be defined"),
            result_update_reference_count: RuntimeLibrary::get_function(
                module,
                "result_update_reference_count",
            )
            .expect("__quantum__rt__result_update_reference_count function must be defined"),
            result_equal: RuntimeLibrary::get_function(module, "result_equal")
                .expect("__quantum__rt__result_equal function must be defined"),

            array_create_1d: RuntimeLibrary::get_function(module, "array_create_1d")
                .expect("__quantum__rt__array_create_1d function must be defined"),
            array_get_size_1d: RuntimeLibrary::get_function(module, "array_get_size_1d")
                .expect("__quantum__rt__array_get_size_1d function must be defined"),
            array_get_element_ptr_1d: RuntimeLibrary::get_function(
                module,
                "array_get_element_ptr_1d",
            )
            .expect("__quantum__rt__array_get_element_ptr_1d function must be defined"),
            array_update_alias_count: RuntimeLibrary::get_function(
                module,
                "array_update_alias_count",
            )
            .expect("__quantum__rt__array_update_alias_count function must be defined"),
            array_update_reference_count: RuntimeLibrary::get_function(
                module,
                "array_update_reference_count",
            )
            .expect("__quantum__rt__array_update_reference_count function must be defined"),
            memory_allocate: RuntimeLibrary::get_function(module, "memory_allocate"),
            message: RuntimeLibrary::get_function(module, "message")
                .expect("__quantum__rt__message function must be defined"),
            string_create: RuntimeLibrary::get_function(module, "string_create")
                .expect("__quantum__rt__string_create function must be defined"),
            string_update_reference_count: RuntimeLibrary::get_function(
                module,
                "string_update_reference_count",
            )
            .expect("__quantum__rt__string_update_reference_count function must be defined"),
            string_concatenate: RuntimeLibrary::get_function(module, "string_concatenate")
                .expect("__quantum__rt__string_concatenate function must be defined"),
            result_to_string: RuntimeLibrary::get_function(module, "result_to_string")
                .expect("__quantum__rt__result_to_string function must be defined"),
            qubit_allocate: RuntimeLibrary::get_function(module, "qubit_allocate")
                .expect("__quantum__rt__qubit_allocate function must be defined"),
            qubit_allocate_array: RuntimeLibrary::get_function(module, "qubit_allocate_array"),
            qubit_release: RuntimeLibrary::get_function(module, "qubit_release")
                .expect("__quantum__rt__qubit_release function must be defined"),
            qubit_release_array: RuntimeLibrary::get_function(module, "qubit_release_array"),
        }
    }

    fn get_function(module: &Module<'ctx>, name: &str) -> Option<FunctionValue<'ctx>> {
        let function_name = format!("__quantum__rt__{}", name);
        let defined_function = module.get_function(&function_name[..]);

        match defined_function {
            None => {
                log::debug!("{} was not defined in the module", function_name);
                None
            }
            Some(value) => Some(value),
        }
    }
}

#[cfg(test)]
mod tests {
    use crate::emit::Context;

    use super::*;

    #[test]
    fn runtime_library_can_be_loaded() {
        let ctx = inkwell::context::Context::create();
        let name = "temp";
        let context = Context::new(&ctx, name);
        let _ = RuntimeLibrary::new(&context.module);
    }
}
