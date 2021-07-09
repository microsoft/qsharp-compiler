// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use crate::emit::Context;
use inkwell::values::{BasicValueEnum, FunctionValue};

pub(crate) fn emit_void_call<'ctx>(
    context: &Context<'ctx>,
    function: FunctionValue<'ctx>,
    args: &[BasicValueEnum<'ctx>],
) {
    let _ = context
        .builder
        .build_call(function, args, "")
        .try_as_basic_value()
        .right()
        .unwrap();
}

pub(crate) fn emit_call_with_return<'ctx>(
    context: &Context<'ctx>,
    function: FunctionValue<'ctx>,
    args: &[BasicValueEnum<'ctx>],
    name: &str,
) -> BasicValueEnum<'ctx> {
    context
        .builder
        .build_call(function, args, name)
        .try_as_basic_value()
        .left()
        .unwrap()
}
