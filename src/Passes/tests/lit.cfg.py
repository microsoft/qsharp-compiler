# -*- Python -*-
import platform
import lit.formats
from lit.llvm import llvm_config
from lit.llvm.subst import ToolSubst
import shutil

config.llvm_tools_dir = os.path.dirname(shutil.which("opt"))
config.name = 'Quantum-Passes'
config.test_format = lit.formats.ShTest(not llvm_config.use_lit_shell)
config.suffixes = ['.ll']
config.test_source_root = os.path.dirname(__file__)
config.excludes = ['inputs', "*/inputs", "**/inputs"]

if platform.system() == 'Darwin':
    tool_substitutions = [
        ToolSubst('%clang', "clang",
                  extra_args=["-isysroot",
                              "`xcrun --show-sdk-path`",
                              "-mlinker-version=0"]),
    ]
else:
    tool_substitutions = [
        ToolSubst('%clang', "clang",
                  )
    ]
llvm_config.add_tool_substitutions(tool_substitutions)
tools = ["opt", "lli", "not", "FileCheck", "clang"]
llvm_config.add_tool_substitutions(tools, config.llvm_tools_dir)
config.substitutions.append(('%shlibext', config.llvm_shlib_ext))
config.substitutions.append(('%shlibdir', config.llvm_shlib_dir))


# References:
# https://github.com/banach-space/llvm-tutor
# http://lists.llvm.org/pipermail/cfe-dev/2016-July/049868.html
# https://github.com/Homebrew/homebrew-core/issues/52461
