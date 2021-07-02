use inkwell::module::Module;
use inkwell::values::FunctionValue;

// result functions
pub const ResultGetZero: &str = "result_get_zero";
pub const ResultGetOne: &str = "result_get_one";
pub const ResultUpdateReferenceCount: &str = "result_update_reference_count";
pub const ResultEqual: &str = "result_equal";

// array functions
pub const ArrayCreate1d: &str = "array_create_1d";
pub const ArrayGetElementPtr1d: &str = "array_get_element_ptr_1d";
pub const ArrayUpdateAliasCount: &str = "array_update_alias_count";

// qubit functions
pub const QubitAllocate: &str = "qubit_allocate";
pub const QubitAllocateArray: &str = "qubit_allocate_array";
pub const QubitRelease: &str = "qubit_release";
pub const QubitReleaseArray: &str = "qubit_release_array";

pub struct RuntimeLibrary<'ctx> {
    pub(crate) ResultGetZero: FunctionValue<'ctx>,
    pub(crate) ResultGetOne: FunctionValue<'ctx>,
    pub(crate) ResultUpdateReferenceCount: FunctionValue<'ctx>,
    pub(crate) ResultEqual: FunctionValue<'ctx>,
    pub(crate) ArrayCreate1d: FunctionValue<'ctx>,
    pub(crate) ArrayGetElementPtr1d: FunctionValue<'ctx>,
    pub(crate) ArrayUpdateAliasCount: FunctionValue<'ctx>,
    pub(crate) QubitAllocate: FunctionValue<'ctx>,
    pub(crate) QubitAllocateArray: FunctionValue<'ctx>,
    pub(crate) QubitRelease: FunctionValue<'ctx>,
    //pub(crate) QubitReleaseArray: FunctionValue<'ctx>,
}

impl<'ctx> RuntimeLibrary<'ctx> {
    pub fn new(module: &Module<'ctx>) -> Self {
        RuntimeLibrary {
            ResultGetZero: RuntimeLibrary::get_function(module, ResultGetZero),
            ResultGetOne: RuntimeLibrary::get_function(module, ResultGetOne),
            ResultUpdateReferenceCount: RuntimeLibrary::get_function(
                module,
                ResultUpdateReferenceCount,
            ),
            ResultEqual: RuntimeLibrary::get_function(module, ResultEqual),

            ArrayCreate1d: RuntimeLibrary::get_function(module, ArrayCreate1d),
            ArrayGetElementPtr1d: RuntimeLibrary::get_function(module, ArrayGetElementPtr1d),
            ArrayUpdateAliasCount: RuntimeLibrary::get_function(module, ArrayUpdateAliasCount),

            QubitAllocate: RuntimeLibrary::get_function(module, QubitAllocate),
            QubitAllocateArray: RuntimeLibrary::get_function(module, QubitAllocateArray),
            QubitRelease: RuntimeLibrary::get_function(module, QubitRelease),
            //QubitReleaseArray: RuntimeLibrary::get_function(module, QubitReleaseArray),
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
    use inkwell::context::Context;

    use crate::emit::ModuleContext;

    use super::*;

    #[test]
    fn runtime_library_can_be_loaded() {
        let ctx = Context::create();
        let name = "temp";
        let module_ctx = ModuleContext::new(&ctx, name);
        let _ = RuntimeLibrary::new(&module_ctx.module);
    }
}
