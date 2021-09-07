// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use inkwell::values::{BasicValue, BasicValueEnum};

use super::{calls, Context};

pub(crate) fn emit_allocate<'ctx>(
    context: &Context<'ctx>,
    result_name: &str,
) -> BasicValueEnum<'ctx> {
    let args = [];
    calls::emit_call_with_return(
        context,
        context.runtime_library.qubit_allocate,
        &args,
        result_name,
    )
}

pub(crate) fn emit_release<'ctx>(context: &Context<'ctx>, qubit: &BasicValueEnum<'ctx>) {
    let args = [qubit.as_basic_value_enum()];
    calls::emit_void_call(context, context.runtime_library.qubit_release, &args);
}
