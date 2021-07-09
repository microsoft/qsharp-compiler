use inkwell::module::Module;
use inkwell::values::FunctionValue;

// result functions
const RESULT_GET_ZERO: &str = "result_get_zero";
const RESULT_GET_ONE: &str = "result_get_one";
const RESULT_UPDATE_REFERENCE_COUNT: &str = "result_update_reference_count";
const RESULT_EQUAL: &str = "result_equal";

// array functions
const ARRAY_CREATE_1D: &str = "array_create_1d";
const ARRAY_GET_ELEMENT_PTR_1D: &str = "array_get_element_ptr_1d";
const ARRAY_UPDATE_ALIAS_COUNT: &str = "array_update_alias_count";
const ARRAY_UPDATE_REFERENCE_COUNT: &str = "array_update_reference_count";

// qubit functions
const QUBIT_ALLOCATE: &str = "qubit_allocate";
const QUBIT_ALLOCATE_ARRAY: &str = "qubit_allocate_array";
const QUBIT_RELEASE: &str = "qubit_release";
const QUBIT_RELEASE_ARRAY: &str = "qubit_release_array";

pub(crate) struct RuntimeLibrary<'ctx> {
    pub(crate) result_get_zero: FunctionValue<'ctx>,
    pub(crate) result_get_one: FunctionValue<'ctx>,
    pub(crate) result_update_reference_count: FunctionValue<'ctx>,
    pub(crate) result_equal: FunctionValue<'ctx>,
    pub(crate) array_create_1d: FunctionValue<'ctx>,
    pub(crate) array_get_element_ptr_1d: FunctionValue<'ctx>,
    pub(crate) array_update_alias_count: FunctionValue<'ctx>,
    pub(crate) array_update_reference_count: FunctionValue<'ctx>,
    pub(crate) qubit_allocate: FunctionValue<'ctx>,
    pub(crate) qubit_allocate_array: FunctionValue<'ctx>,
    pub(crate) qubit_release: FunctionValue<'ctx>,
    //pub(crate) qubit_release_array: FunctionValue<'ctx>,
}

impl<'ctx> RuntimeLibrary<'ctx> {
    pub fn new(module: &Module<'ctx>) -> Self {
        RuntimeLibrary {
            result_get_zero: RuntimeLibrary::get_function(module, RESULT_GET_ZERO),
            result_get_one: RuntimeLibrary::get_function(module, RESULT_GET_ONE),
            result_update_reference_count: RuntimeLibrary::get_function(
                module,
                RESULT_UPDATE_REFERENCE_COUNT,
            ),
            result_equal: RuntimeLibrary::get_function(module, RESULT_EQUAL),

            array_create_1d: RuntimeLibrary::get_function(module, ARRAY_CREATE_1D),
            array_get_element_ptr_1d: RuntimeLibrary::get_function(
                module,
                ARRAY_GET_ELEMENT_PTR_1D,
            ),
            array_update_alias_count: RuntimeLibrary::get_function(
                module,
                ARRAY_UPDATE_ALIAS_COUNT,
            ),
            array_update_reference_count: RuntimeLibrary::get_function(
                module,
                ARRAY_UPDATE_REFERENCE_COUNT,
            ),
            qubit_allocate: RuntimeLibrary::get_function(module, QUBIT_ALLOCATE),
            qubit_allocate_array: RuntimeLibrary::get_function(module, QUBIT_ALLOCATE_ARRAY),
            qubit_release: RuntimeLibrary::get_function(module, QUBIT_RELEASE),
            //qubit_release_array: RuntimeLibrary::get_function(module, QUBIT_RELEASE_ARRAY),
        }
    }

    fn get_function(module: &Module<'ctx>, name: &str) -> FunctionValue<'ctx> {
        let function_name = format!("__quantum__rt__{}", name);
        if let Some(defined_function) = module.get_function(&function_name[..]) {
            return defined_function;
        }
        panic!("{} was not defined in the module", function_name);
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
