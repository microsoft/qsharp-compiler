use inkwell::AddressSpace;
use inkwell::memory_buffer::MemoryBuffer;
use inkwell::module::Linkage;
use inkwell::types::BasicType;
use inkwell::types::BasicTypeEnum;
use inkwell::types::FloatType;
use inkwell::types::IntType;
use inkwell::types::PointerType;
use inkwell::types::StructType;
use inkwell::values::BasicValue;
use inkwell::values::GlobalValue;
use inkwell::values::PointerValue;
use inkwell::module::Module;

use inkwell::context::Context;
use std::path::Path;

mod typenames;

struct Constants<'ctx> {
    Unit:  PointerValue<'ctx>,
    PauliI: GlobalValue<'ctx>,
    PauliX: GlobalValue<'ctx>,
    PauliY: GlobalValue<'ctx>,
    PauliZ: GlobalValue<'ctx>,
    EmptyRange: GlobalValue<'ctx>,
}

// impl<'ctx> Constants<'ctx> {
//     fn new(ctx: &'ctx TranspilerContext) -> Self {
//         Constants {
//             Unit : ctx.types.tuple.get_undef(),
//             PauliI : ctx.add_global(ctx.types.pauli, "PauliI", &ctx.types.pauli.const_int(0, false)),
//             PauliX : ctx.add_global(ctx.types.pauli, "PauliX", &ctx.types.pauli.const_int(1, false)),
//             PauliY : ctx.add_global(ctx.types.pauli, "PauliY", &ctx.types.pauli.const_int(2, false)),
//             PauliZ : ctx.add_global(ctx.types.pauli, "PauliZ", &ctx.types.pauli.const_int(3, false)),
//             // todo: range 0, 1, -1, the -1 isn't allowed
//             EmptyRange : ctx.add_global(ctx.types.range, "EmptyRange", &ctx.context.const_struct(&[ctx.context.i64_type().const_int(0, true).as_basic_value_enum(), ctx.context.i64_type().const_int(1, true).as_basic_value_enum(), ctx.context.i64_type().const_int(1, true).as_basic_value_enum()], false))
//         }
//     }
// }

pub enum Register {
    Quantum {name: String, size: u64},
    Classical {name: String, index: u64},
}

// https://github.com/microsoft/qsharp-language/blob/ageller/profile/Specifications/QIR/Base-Profile.md
pub enum Instruction {
    Cx,
    Cz,
    H,
    Mz,
    Reset,
    Rx,
    Ry,
    Rz,
    S,
    Sdg,
    T,
    Tdg,
    X,
    Y,
    Z
}

pub struct SemanticModel
{
    name: String,
    registers: Vec<Register>,
    instructions: Vec<Instruction>,
}

impl SemanticModel {
    pub fn new(name: String) -> Self {
        SemanticModel { 
            name : name,
            registers : vec![],
            instructions: vec![],
         }
    }
    pub fn add_reg(&mut self, reg: Register) {
        self.registers.push(reg);
    }
    pub fn add_inst(&mut self, inst: Instruction) {
        self.instructions.push(inst);
    }
}

struct Types<'ctx> {
    int : IntType<'ctx>,
    double : FloatType<'ctx>,
    bool : IntType<'ctx>,
    pauli : IntType<'ctx>,
    range: StructType<'ctx>,
    result: PointerType<'ctx>,
    qubit: PointerType<'ctx>,
    string: PointerType<'ctx>,
    big_int: PointerType<'ctx>,
    tuple: PointerType<'ctx>,
    array: PointerType<'ctx>,
    callable: PointerType<'ctx>,
}
fn create_named_opaque_struct<'ctx>(context: &'ctx Context, name: &str, field_types: &[BasicTypeEnum]) -> StructType<'ctx> {
    let s = context.opaque_struct_type(name);
    s.set_body(field_types, false);
    s
}
impl<'ctx> Types<'ctx> {

    fn new(context: &'ctx Context) -> Self {
        Types {
            int : context.i64_type(),
            double : context.f64_type(),
            bool : context.bool_type(),
            pauli : context.custom_width_int_type(2),
            
            range : create_named_opaque_struct(&context, typenames::Range, &[context.i64_type().into(), context.i64_type().into(), context.i64_type().into()]),
            
            result : context.opaque_struct_type(typenames::Result).ptr_type(AddressSpace::Generic),
            qubit : context.opaque_struct_type(typenames::Qubit).ptr_type(AddressSpace::Generic),
            string : context.opaque_struct_type(typenames::String).ptr_type(AddressSpace::Generic),
            big_int : context.opaque_struct_type(typenames::BigInt).ptr_type(AddressSpace::Generic),
            tuple : context.opaque_struct_type(typenames::Tuple).ptr_type(AddressSpace::Generic),
            array : context.opaque_struct_type(typenames::Array).ptr_type(AddressSpace::Generic),
            callable : context.opaque_struct_type(typenames::Callable).ptr_type(AddressSpace::Generic),
        }
    }
}
pub struct EmitterContext<'ctx>
{
    pub context: &'ctx inkwell::context::Context,
    pub module: inkwell::module::Module<'ctx>,
    pub builder: inkwell::builder::Builder<'ctx>,
    //types : Types<'ctx>,
}

pub struct Emitter {}
impl Emitter {
    pub fn write(model: &SemanticModel, file_name: &str) {
        let ctx = Context::create();
        let emitter_ctx = EmitterContext::new(&ctx, model.name.as_str());
        
        emitter_ctx.add_boilerplate();
        let ns = "QuantumApplication";
        let method = "Run";
        let entrypoint_name = format!("@{}__{}__body", ns, method);
        let entrypoint = emitter_ctx.module.get_function(&entrypoint_name).unwrap();
        while let Some(bb) = entrypoint.get_last_basic_block() {
            unsafe{
                bb.delete().unwrap();
            }
        }
        // convert semantic model to QIR and inject body
        emitter_ctx.module.verify().unwrap();
        //emitter_ctx.emit_bitcode("module.bc");
        emitter_ctx.emit_ir(file_name);
    }
}

impl<'ctx> EmitterContext<'ctx> {
    
    pub fn new(context: &'ctx Context, name: &'ctx str) -> Self {
        let builder = context.create_builder();
        //let types = Types::new(&context);
        EmitterContext { 
            context: context,
            module: EmitterContext::parse_bitcode_from_buffer(&context, name),
            builder: builder,
            //types : types,
         }
    }

    pub fn parse_bitcode_from_buffer(context: &'ctx Context, name: &'ctx str) -> Module<'ctx> {
        let module_contents = include_bytes!("module.bc");
        let buffer = MemoryBuffer::create_from_memory_range_copy(module_contents, name);
        let module = Module::parse_bitcode_from_buffer(&buffer, context).unwrap();
        module
    }

    pub fn add_global<T: BasicType<'ctx>>(&self, type_: T, name: &str, value: &dyn BasicValue<'ctx>) -> GlobalValue<'ctx> {
        let x = self.module.add_global(type_, Some(AddressSpace::Const), name);
        x.set_constant(true);
        x.set_linkage(Linkage::Internal);
        
        x.set_initializer(value);
        x
    }

    pub fn add_boilerplate(&self) {
        let void_type = self.context.void_type();
        let fn_type = void_type.fn_type(&[], false);
        let fn_val = self.module.add_function("my_fn", fn_type, None);
        let basic_block = self.context.append_basic_block(fn_val, "entry");
        self.builder.position_at_end(basic_block);
        self.builder.build_return(None);
    }
    
    pub fn emit_bitcode(&self,  file_path: &str) {
        let bitcode_path = Path::new(file_path);
        self.module.write_bitcode_to_path(&bitcode_path);
    }
    
    pub fn emit_ir(&self,  file_path: &str) {
        let ir_path = Path::new(file_path);
        if let Err(_) = self.module.print_to_file(ir_path){
            todo!()
        }
    }
}




#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn it_works() {
        println!("{}", "test");
    }
}
