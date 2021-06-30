use inkwell::values::GlobalValue;
use inkwell::values::PointerValue;

struct Constants<'ctx> {
    Unit: PointerValue<'ctx>,
    PauliI: GlobalValue<'ctx>,
    PauliX: GlobalValue<'ctx>,
    PauliY: GlobalValue<'ctx>,
    PauliZ: GlobalValue<'ctx>,
    EmptyRange: GlobalValue<'ctx>,
}
use super::types::Types;
use super::ModuleContext;

impl<'ctx> Constants<'ctx> {
    fn new(ctx: &ModuleContext<'ctx>, types: &Types<'ctx>) -> Self {
        Constants {
            Unit: types.tuple.const_null(),
            PauliI: Constants::get_global(ctx, "PauliI"),
            PauliX: Constants::get_global(ctx, "PauliX"),
            PauliY: Constants::get_global(ctx, "PauliY"),
            PauliZ: Constants::get_global(ctx, "PauliZ"),
            EmptyRange: Constants::get_global(ctx, "EmptyRange"),
        }
    }

    fn get_global(module_ctx: &ModuleContext<'ctx>, name: &str) -> GlobalValue<'ctx> {
        if let Some(defined_global) = module_ctx.module.get_global(name) {
            return defined_global;
        }
        panic!("{} global constant was not defined in the module", name);
    }
}

#[cfg(test)]
mod tests {
    use inkwell::context::Context;

    use crate::emit::ModuleContext;

    use super::*;

    #[test]
    fn constants_can_be_loaded() {
        let ctx = Context::create();
        let name = "temp";
        let module_ctx = ModuleContext::new(&ctx, name);
        let types = Types::new(&module_ctx.context, &module_ctx.module);
        let _ = Constants::new(&module_ctx, &types);
    }
}
