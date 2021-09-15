use inkwell::execution_engine::JitFunction;
use inkwell::OptimizationLevel;

type SumU64 = unsafe extern "C" fn(u64, u64) -> u64;

pub struct Context<'ctx> {
    pub(crate) context: &'ctx inkwell::context::Context,
    pub(crate) module: inkwell::module::Module<'ctx>,
    pub(crate) builder: inkwell::builder::Builder<'ctx>,
    pub(crate) execution_engine: inkwell::execution_engine::ExecutionEngine<'ctx>,
}

impl<'ctx> Context<'ctx> {
    pub fn new(context: &'ctx inkwell::context::Context, name: &'ctx str) -> Self {
        let builder = context.create_builder();
        let module = context.create_module(name);

        let execution_engine = module
            .create_jit_execution_engine(OptimizationLevel::None)
            .expect("Could not create JIT Engine");
        Context {
            builder,
            module,
            context,
            execution_engine,
        }
    }

    fn jit_compile_sumu64(&self, name: &'ctx str) -> Option<JitFunction<'ctx, SumU64>> {
        let i64_type = self.context.i64_type();
        let fn_type = i64_type.fn_type(&[i64_type.into(), i64_type.into()], false);
        let function = self.module.add_function(name, fn_type, None);
        let basic_block = self.context.append_basic_block(function, "entry");

        self.builder.position_at_end(basic_block);

        let x = function.get_nth_param(0)?.into_int_value();
        let y = function.get_nth_param(1)?.into_int_value();

        let sum = self.builder.build_int_add(x, y, name);

        self.builder.build_return(Some(&sum));

        unsafe { self.execution_engine.get_function(name).ok() }
    }
}

#[cfg(test)]
mod tests {
    use crate::jit::Context;

    #[test]
    fn jit_compilation_of_simple_function() {
        let ctx = inkwell::context::Context::create();
        let name = "jit_compilation_of_simple_function";
        let context = Context::new(&ctx, name);

        let sum = context
            .jit_compile_sumu64(name)
            .expect("Unable to JIT compile sum function");

        let x = 1u64;
        let y = 2u64;

        unsafe {
            assert_eq!(sum.call(x, y), x + y);
        }
    }
}
