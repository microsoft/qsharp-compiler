use qirlib::{
    emit::Emitter,
    interop::{
        ClassicalRegister, Controlled, Instruction, QuantumRegister, Rotated, SemanticModel, Single,
    },
};
use std::io::{self, Write};
use std::path::Path;
use std::{env, fs};

#[test]
fn bell_circuit_with_measurement() {
    execute(
        "bell_measure",
        write_bell_measure,
        vec!["[[Zero, Zero]]", "[[One, One]]"],
    );
}

#[test]
fn bell_circuit_no_measurement() {
    execute("bell_no_measure", write_bell_no_measure, vec!["[]"]);
}

#[test]
fn empty_model() {
    execute("empty", write_empty_model, vec!["[]"]);
}

#[test]
fn model_with_only_qubit_allocations() {
    execute(
        "model_with_only_qubit_allocations",
        write_model_with_only_qubit_allocations,
        vec!["[]"],
    );
}
#[test]
fn model_with_only_result_allocations() {
    execute(
        "model_with_only_result_allocations",
        write_model_with_only_result_allocations,
        vec!["[[Zero, Zero, Zero, Zero], [Zero, Zero, Zero], [Zero, Zero]]"],
    );
}

#[test]
fn model_with_no_instructions() {
    execute(
        "model_with_no_instructions",
        write_model_with_no_instructions,
        vec!["[[Zero, Zero]]"],
    );
}
#[test]
fn model_with_single_qubit_instructions() {
    execute(
        "model_with_single_qubit_instructions",
        write_model_with_single_qubit_instructions,
        vec!["[]"],
    );
}
#[test]
fn single_qubit_model_with_measurement() {
    execute(
        "single_qubit_model_with_measurement",
        write_single_qubit_model_with_measurement,
        vec!["[[Zero]]"],
    );
}
#[test]
fn model_with_instruction_cx() {
    execute(
        "model_with_instruction_cx",
        write_model_with_instruction_cx,
        vec!["[]"],
    );
}

#[test]
fn model_with_instruction_cz() {
    execute(
        "model_with_instruction_cz",
        write_model_with_instruction_cz,
        vec!["[]"],
    );
}
#[test]
fn bernstein_vazirani() {
    execute(
        "bernstein_vazirani",
        write_bernstein_vazirani,
        vec!["[[Zero, Zero, Zero, Zero, Zero]]"],
    );
}

fn execute(name: &str, generator: fn(&str) -> (), expected_results: Vec<&str>) {
    // set up test dir
    // todo: create new random tmp dir. The copies for now overwrite
    let test_dir = format!("{}/{}", env::temp_dir().to_str().unwrap(), "tests");
    if let Err(_) = fs::create_dir(test_dir.as_str()) {
        // todo: check if the dir exists. Rust implementation returns an error
        // instead of being idempotent.
    }

    // generate the QIR
    let ir_path = format!("{}/{}.ll", test_dir.as_str(), name.replace(" ", "_"));
    generator(ir_path.as_str());

    let app = format!("{}/{}", test_dir.as_str(), name);

    let manifest_dir = env::var_os("CARGO_MANIFEST_DIR")
        .expect("")
        .to_str()
        .unwrap()
        .to_owned();

    let runtimes = format!(
        "{}/packages/microsoft.quantum.qir.runtime/0.18.2106148911-alpha/runtimes",
        manifest_dir.as_str()
    );
    let native = format!("{}/linux-x64/native", runtimes);
    let include = format!("{}/any/native/include", runtimes);

    let simulators = format!(
        "{}/packages/microsoft.quantum.simulators/0.18.2106148911/runtimes",
        manifest_dir.as_str()
    );
    let simulators_native = format!("{}/linux-x64/native", simulators);

    copy_files(&native, &test_dir);
    copy_files(&include, &test_dir);
    // todo: this is fixed in new release so that the rename isn't needed.
    copy_files_and_rename_libs(&simulators_native, &test_dir);

    let mut command = std::process::Command::new("clang++-11");
    command
        .arg("-o")
        .arg(app.as_str())
        .arg(ir_path)
        .arg(format!("{}/tests/main.cpp", manifest_dir.as_str()))
        .arg(format!("-I{}", include))
        .arg(format!("-L{}", native))
        .args([
            "-lMicrosoft.Quantum.Qir.Runtime",
            "-lMicrosoft.Quantum.Qir.QSharp.Core",
            "-lMicrosoft.Quantum.Qir.QSharp.Foundation",
        ]);

    println!("{:?}", command);
    let output = command.output().expect("failed to execute process");

    println!("status: {}", output.status);
    io::stdout().write_all(&output.stdout).unwrap();
    io::stderr().write_all(&output.stderr).unwrap();

    assert!(output.status.success());

    execute_circuit(app.as_str(), expected_results);
}

fn copy_files(source: &String, target: &String) {
    if let Ok(entries) = fs::read_dir(source.as_str()) {
        for path in entries {
            let file_name = path.unwrap().path();
            if file_name.is_file() {
                let file = file_name.file_name().unwrap().to_str().unwrap();
                let src = format!("{}/{}", source.as_str(), file);
                let dst = format!("{}/{}", target.as_str(), file);

                std::fs::copy(src.as_str(), dst.as_str()).expect(
                    format!("Failed to copy {} to {}", src.as_str(), dst.as_str()).as_str(),
                );
            }
        }
    }
}

fn copy_files_and_rename_libs(source: &String, target: &String) {
    if let Ok(entries) = fs::read_dir(source.as_str()) {
        for path in entries {
            let file_name = path.unwrap().path();
            if file_name.is_file() {
                let name = file_name.file_name().unwrap().to_str().unwrap();
                let stem = file_name.file_stem().unwrap().to_str().unwrap();
                let src = format!("{}/{}", source.as_str(), name);
                let dst = format!("{}/lib{}.so", target.as_str(), stem);

                std::fs::copy(src.as_str(), dst.as_str()).expect(
                    format!("Failed to copy {} to {}", src.as_str(), dst.as_str()).as_str(),
                );
            }
        }
    }
}

fn execute_circuit(app: &str, expected_results: Vec<&str>) {
    let parent = String::from(Path::new(app).parent().unwrap().to_str().unwrap());
    let mut ld_path = parent.clone();
    if let Ok(existing_value) = env::var("LD_LIBRARY_PATH") {
        ld_path = format!("{}:{}", parent.as_str(), existing_value);
    }

    let mut command = std::process::Command::new(app);
    command.env("LD_LIBRARY_PATH", ld_path.as_str());
    println!("{:?}", command);
    let output = command.output().expect("failed to execute process");

    println!("status: {}", output.status);
    let stdout = String::from_utf8(output.stdout).unwrap().trim().to_owned();
    let stderr = String::from_utf8(output.stderr).unwrap().trim().to_owned();

    println!("out: {}", stdout.as_str());

    assert!(output.status.success());
    assert!(expected_results.iter().any(|&x| x == stdout.as_str()));
}

fn write_empty_model(file_name: &str) {
    let name = String::from("empty");
    let model = SemanticModel::new(name);
    Emitter::write(&model, file_name).unwrap();
}
fn write_model_with_single_qubit_instructions(file_name: &str) {
    let name = String::from("model_with_single_qubit_instructions");
    let mut model = SemanticModel::new(name);
    model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());

    model.add_inst(Instruction::H(Single::new(String::from("qr0"))));
    model.add_inst(Instruction::Reset(Single::new(String::from("qr0"))));
    model.add_inst(Instruction::Rx(Rotated::new(15.0, String::from("qr0"))));
    model.add_inst(Instruction::Ry(Rotated::new(16.0, String::from("qr0"))));
    model.add_inst(Instruction::Rz(Rotated::new(17.0, String::from("qr0"))));
    model.add_inst(Instruction::S(Single::new(String::from("qr0"))));
    model.add_inst(Instruction::Sdg(Single::new(String::from("qr0"))));
    model.add_inst(Instruction::T(Single::new(String::from("qr0"))));
    model.add_inst(Instruction::Tdg(Single::new(String::from("qr0"))));

    Emitter::write(&model, file_name).unwrap();
}
fn write_model_with_instruction_cx(file_name: &str) {
    let name = String::from("model_with_instruction_cx");
    let mut model = SemanticModel::new(name);
    model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());
    model.add_reg(QuantumRegister::new(String::from("qr"), 1).as_register());

    model.add_inst(Instruction::Cx(Controlled::new(
        String::from("qr0"),
        String::from("qr1"),
    )));

    Emitter::write(&model, file_name).unwrap();
}

fn write_model_with_instruction_cz(file_name: &str) {
    let name = String::from("model_with_instruction_cz");
    let mut model = SemanticModel::new(name);
    model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());
    model.add_reg(QuantumRegister::new(String::from("qr"), 1).as_register());

    model.add_inst(Instruction::Cz(Controlled::new(
        String::from("qr0"),
        String::from("qr1"),
    )));

    Emitter::write(&model, file_name).unwrap();
}
fn write_model_with_only_qubit_allocations(file_name: &str) {
    let name = String::from("model_with_only_qubit_allocations");
    let mut model = SemanticModel::new(name);
    model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());
    model.add_reg(QuantumRegister::new(String::from("qr"), 1).as_register());
    Emitter::write(&model, file_name).unwrap();
}
fn write_model_with_only_result_allocations(file_name: &str) {
    let name = String::from("model_with_only_result_allocations");
    let mut model = SemanticModel::new(name);
    model.add_reg(ClassicalRegister::new(String::from("qa"), 4).as_register());
    model.add_reg(ClassicalRegister::new(String::from("qb"), 3).as_register());
    model.add_reg(ClassicalRegister::new(String::from("qc"), 2).as_register());
    Emitter::write(&model, file_name).unwrap();
}
fn write_model_with_no_instructions(file_name: &str) {
    let name = String::from("model_with_no_instructions");
    let mut model = SemanticModel::new(name);
    model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());
    model.add_reg(QuantumRegister::new(String::from("qr"), 1).as_register());
    model.add_reg(ClassicalRegister::new(String::from("qc"), 2).as_register());
    Emitter::write(&model, file_name).unwrap();
}
fn write_bell_no_measure(file_name: &str) {
    let name = String::from("Bell circuit");
    let mut model = SemanticModel::new(name);
    model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());
    model.add_reg(QuantumRegister::new(String::from("qr"), 1).as_register());

    model.add_inst(Instruction::H(Single::new(String::from("qr0"))));
    model.add_inst(Instruction::Cx(Controlled::new(
        String::from("qr0"),
        String::from("qr1"),
    )));
    Emitter::write(&model, file_name).unwrap();
}

fn write_single_qubit_model_with_measurement(file_name: &str) {
    let name = String::from("single_qubit_model_with_measurement");
    let mut model = SemanticModel::new(name);
    model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());
    model.add_reg(ClassicalRegister::new(String::from("qc"), 1).as_register());

    model.add_inst(Instruction::M {
        qubit: String::from("qr0"),
        target: String::from("qc0"),
    });

    Emitter::write(&model, file_name).unwrap();
}

fn write_bell_measure(file_name: &str) {
    let name = String::from("Bell circuit");
    let mut model = SemanticModel::new(name);
    model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());
    model.add_reg(QuantumRegister::new(String::from("qr"), 1).as_register());
    model.add_reg(ClassicalRegister::new(String::from("qc"), 2).as_register());

    model.add_inst(Instruction::H(Single::new(String::from("qr0"))));
    model.add_inst(Instruction::Cx(Controlled::new(
        String::from("qr0"),
        String::from("qr1"),
    )));
    model.add_inst(Instruction::M {
        qubit: String::from("qr0"),
        target: String::from("qc0"),
    });
    model.add_inst(Instruction::M {
        qubit: String::from("qr1"),
        target: String::from("qc1"),
    });
    Emitter::write(&model, file_name).unwrap();
}

fn write_bernstein_vazirani(file_name: &str) {
    let name = String::from("Bernstein-Vazirani circuit");
    let mut model = SemanticModel::new(name);
    model.add_reg(QuantumRegister::new(String::from("input_"), 0).as_register());
    model.add_reg(QuantumRegister::new(String::from("input_"), 1).as_register());
    model.add_reg(QuantumRegister::new(String::from("input_"), 2).as_register());
    model.add_reg(QuantumRegister::new(String::from("input_"), 3).as_register());
    model.add_reg(QuantumRegister::new(String::from("input_"), 4).as_register());
    model.add_reg(QuantumRegister::new(String::from("target_"), 0).as_register());
    model.add_reg(ClassicalRegister::new(String::from("output_"), 5).as_register());

    model.add_inst(Instruction::X(Single::new(String::from("target_0"))));
    model.add_inst(Instruction::H(Single::new(String::from("input_0"))));
    model.add_inst(Instruction::H(Single::new(String::from("input_1"))));
    model.add_inst(Instruction::H(Single::new(String::from("input_2"))));
    model.add_inst(Instruction::H(Single::new(String::from("input_3"))));
    model.add_inst(Instruction::H(Single::new(String::from("input_4"))));
    model.add_inst(Instruction::H(Single::new(String::from("target_0"))));

    // random chosen
    model.add_inst(Instruction::Cx(Controlled::new(
        String::from("input_2"),
        String::from("target_0"),
    )));
    model.add_inst(Instruction::Cx(Controlled::new(
        String::from("input_2"),
        String::from("target_0"),
    )));
    model.add_inst(Instruction::H(Single::new(String::from("input_0"))));
    model.add_inst(Instruction::H(Single::new(String::from("input_1"))));
    model.add_inst(Instruction::H(Single::new(String::from("input_2"))));
    model.add_inst(Instruction::H(Single::new(String::from("input_3"))));
    model.add_inst(Instruction::H(Single::new(String::from("input_4"))));
    model.add_inst(Instruction::M {
        qubit: String::from("input_0"),
        target: String::from("output_0"),
    });
    model.add_inst(Instruction::M {
        qubit: String::from("input_1"),
        target: String::from("output_1"),
    });
    model.add_inst(Instruction::M {
        qubit: String::from("input_2"),
        target: String::from("output_2"),
    });
    model.add_inst(Instruction::M {
        qubit: String::from("input_3"),
        target: String::from("output_3"),
    });
    model.add_inst(Instruction::M {
        qubit: String::from("input_4"),
        target: String::from("output_4"),
    });
    model.add_inst(Instruction::Reset(Single::new(String::from("input_0"))));
    model.add_inst(Instruction::Reset(Single::new(String::from("input_1"))));
    model.add_inst(Instruction::Reset(Single::new(String::from("input_2"))));
    model.add_inst(Instruction::Reset(Single::new(String::from("input_3"))));
    model.add_inst(Instruction::Reset(Single::new(String::from("input_4"))));
    Emitter::write(&model, file_name).unwrap();
}
