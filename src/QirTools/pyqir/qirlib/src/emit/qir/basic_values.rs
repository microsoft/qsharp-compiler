use inkwell::values::{BasicValue, BasicValueEnum};

use crate::emit::Context;

pub(crate) fn f64_to_f64<'ctx>(context: &Context<'ctx>, value: &f64) -> BasicValueEnum<'ctx> {
    context
        .types
        .double
        .const_float(value.clone())
        .as_basic_value_enum()
}

pub(crate) fn u64_to_i32<'ctx>(context: &Context<'ctx>, value: u64) -> BasicValueEnum<'ctx> {
    context
        .context
        .i32_type()
        .const_int(value, false)
        .as_basic_value_enum()
}

pub(crate) fn i64_to_i32<'ctx>(context: &Context<'ctx>, value: i64) -> BasicValueEnum<'ctx> {
    // convert to capture negative values.
    let target: u64 = value as u64;

    context
        .context
        .i32_type()
        .const_int(target, false)
        .as_basic_value_enum()
}

pub(crate) fn u64_to_i64<'ctx>(context: &Context<'ctx>, value: u64) -> BasicValueEnum<'ctx> {
    context
        .types
        .int
        .const_int(value, false)
        .as_basic_value_enum()
}
