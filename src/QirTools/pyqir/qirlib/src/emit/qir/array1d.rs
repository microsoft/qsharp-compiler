use inkwell::values::{BasicValue, BasicValueEnum};
use inkwell::AddressSpace;

use super::EmitterContext;

pub(crate) fn emit_array_1d<'ctx>(
    context: &EmitterContext<'ctx>,
    name: &str,
    size: u64,
) -> BasicValueEnum<'ctx> {
    let sub_result_name = format!("{}", &name[..]);
    let sub_result = emit_array_allocate1d(&context, 8, size, sub_result_name.as_str());

    for i in 0..size {
        let cast = get_bitcast_result_pointer_array_element(
            context,
            i,
            &sub_result,
            sub_result_name.as_str(),
        );
        let zero = context
            .module_ctx
            .builder
            .build_call(
                context.runtime_library.ResultGetZero,
                &[],
                format!("zero_{}", i).as_str(),
            )
            .try_as_basic_value()
            .left()
            .unwrap();
        let one = context
            .context
            .i32_type()
            .const_int(1, false)
            .as_basic_value_enum();
        context.module_ctx.builder.build_call(
            context.runtime_library.ResultUpdateReferenceCount,
            &[zero, one],
            name,
        );
        context
            .module_ctx
            .builder
            .build_store(cast.into_pointer_value(), zero);
    }
    
    context.module_ctx.builder.build_call(
        context.runtime_library.ArrayUpdateAliasCount,
        &[
            sub_result,
            context
                .context
                .i32_type()
                .const_int(1, false)
                .as_basic_value_enum(),
        ],
        "",
    );
    sub_result
}

fn get_bitcast_array_pointer_element<'ctx>(
    context: &EmitterContext<'ctx>,
    i: u64,
    sub_result: &BasicValueEnum<'ctx>,
    sub_result_name: &str,
) -> BasicValueEnum<'ctx> {
    let element_raw_ptr_name = format!("{}_{}_raw", sub_result_name, i);
    let sub_result_element_ptr = emit_array_get_element_ptr_1d(
        context,
        i,
        sub_result.as_basic_value_enum(),
        element_raw_ptr_name.as_str(),
    );

    let element_result_ptr_name = format!("{}_result_{}", sub_result_name, i);
    let target_type = context.types.array;
    let cast = context.module_ctx.builder.build_bitcast(
        sub_result_element_ptr,
        target_type.ptr_type(AddressSpace::Generic),
        element_result_ptr_name.as_str(),
    );
    cast
}

fn get_bitcast_array_element<'ctx>(
    context: &EmitterContext<'ctx>,
    i: u64,
    sub_result: &BasicValueEnum<'ctx>,
    sub_result_name: &str,
) -> BasicValueEnum<'ctx> {
    let element_raw_ptr_name = format!("{}_{}_raw", sub_result_name, i);
    let sub_result_element_ptr = emit_array_get_element_ptr_1d(
        context,
        i,
        sub_result.as_basic_value_enum(),
        element_raw_ptr_name.as_str(),
    );

    let element_result_ptr_name = format!("{}_result_{}", sub_result_name, i);
    let target_type = context.types.array;
    let cast = context.module_ctx.builder.build_bitcast(
        sub_result_element_ptr,
        target_type,
        element_result_ptr_name.as_str(),
    );
    cast
}

fn get_bitcast_result_pointer_array_element<'ctx>(
    context: &EmitterContext<'ctx>,
    i: u64,
    sub_result: &BasicValueEnum<'ctx>,
    sub_result_name: &str,
) -> BasicValueEnum<'ctx> {
    let element_raw_ptr_name = format!("{}_{}_raw", sub_result_name, i);
    let sub_result_element_ptr = emit_array_get_element_ptr_1d(
        context,
        i,
        sub_result.as_basic_value_enum(),
        element_raw_ptr_name.as_str(),
    );

    let element_result_ptr_name = format!("{}_result_{}", sub_result_name, i);
    let target_type = context.types.result;
    let cast = context.module_ctx.builder.build_bitcast(
        sub_result_element_ptr,
        target_type.ptr_type(AddressSpace::Generic),
        element_result_ptr_name.as_str(),
    );
    cast
}

pub(crate) fn emit_empty_result_array_allocate1d<'ctx>(
    context: &EmitterContext<'ctx>,
    result_name: &str,
) -> BasicValueEnum<'ctx> {
    let results = emit_array_allocate1d(&context, 8, 0, &result_name[..]);
    results
}

pub(crate) fn emit_array_allocate1d<'ctx>(
    emitter_ctx: &EmitterContext<'ctx>,
    bits: u64,
    length: u64,
    result_name: &str,
) -> BasicValueEnum<'ctx> {
    let args = &[
        emitter_ctx
            .context
            .i32_type()
            .const_int(bits, false)
            .as_basic_value_enum(),
        emitter_ctx
            .types
            .int
            .const_int(length, false)
            .as_basic_value_enum(),
    ];
    let lhs = emitter_ctx
        .module_ctx
        .builder
        .build_call(emitter_ctx.runtime_library.ArrayCreate1d, args, result_name)
        .try_as_basic_value();
    lhs.left().unwrap()
}

pub(crate) fn emit_array_get_element_ptr_1d<'ctx>(
    emitter_ctx: &EmitterContext<'ctx>,
    index: u64,
    target: BasicValueEnum<'ctx>,
    result_name: &str,
) -> BasicValueEnum<'ctx> {
    let args = &[
        target,
        emitter_ctx
            .context
            .i64_type()
            .const_int(index, false)
            .as_basic_value_enum(),
    ];
    let lhs = emitter_ctx
        .module_ctx
        .builder
        .build_call(
            emitter_ctx.runtime_library.ArrayGetElementPtr1d,
            args,
            result_name,
        )
        .try_as_basic_value();
    lhs.left().unwrap()
}

pub(crate) fn set_elements<'ctx>(
    context: &EmitterContext<'ctx>,
    results: &BasicValueEnum<'ctx>,
    sub_results: Vec<BasicValueEnum<'ctx>>,
    name: &str,
) -> () {
    for i in 0..sub_results.len() {
        let result_indexed_name = format!("{}_result_tmp", &name[..]);
        let result_indexed = get_bitcast_array_pointer_element(
            context,
            i as u64,
            &results,
            result_indexed_name.as_str(),
        );
        
        let _ = context
            .module_ctx
            .builder
            .build_store(result_indexed.into_pointer_value(), sub_results[i]);
    }
}
