// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use crate::{
    emit::Context,
    emit::{populate_context, ContextType},
};

use super::{pyjit::run_ctx, SemanticModel};

pub fn write(model: &SemanticModel, file_name: &str) -> Result<(), String> {
    let ctx = inkwell::context::Context::create();
    let context = populate_context(&ctx, &model)?;

    context.emit_ir(file_name)?;

    Ok(())
}

pub fn get_ir_string(model: &SemanticModel) -> Result<String, String> {
    let ctx = inkwell::context::Context::create();
    let context = populate_context(&ctx, &model)?;

    let ir = context.get_ir_string();

    Ok(ir)
}

pub fn get_bitcode_base64_string(model: &SemanticModel) -> Result<String, String> {
    let ctx = inkwell::context::Context::create();
    let context = populate_context(&ctx, &model)?;

    let b64 = context.get_bitcode_base64_string();

    Ok(b64)
}

pub fn run_module(module: String) -> Result<SemanticModel, String> {
    let ctx = inkwell::context::Context::create();
    let context_type = ContextType::File(&module);
    let context = Context::new(&ctx, context_type)?;
    let model = run_ctx(context)?;
    Ok(model)
}

pub fn run(model: &SemanticModel) -> Result<SemanticModel, String> {
    let ctx = inkwell::context::Context::create();
    let context = populate_context(&ctx, &model).unwrap();
    let model = run_ctx(context)?;
    Ok(model)
}
