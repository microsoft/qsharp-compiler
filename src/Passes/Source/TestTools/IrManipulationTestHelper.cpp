// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "TestTools/IrManipulationTestHelper.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    namespace
    {
        inline void ltrim(std::string& str)
        {
            str.erase(str.begin(), std::find_if(str.begin(), str.end(), [](uint8_t ch) { return !std::isspace(ch); }));
        }

        inline void rtrim(std::string& str)
        {
            str.erase(
                std::find_if(str.rbegin(), str.rend(), [](uint8_t ch) { return !std::isspace(ch); }).base(), str.end());
        }

        inline void trim(std::string& s)
        {
            ltrim(s);
            rtrim(s);
        }

    } // namespace

    IrManipulationTestHelper::IrManipulationTestHelper()
    {
        pass_builder_.registerModuleAnalyses(module_analysis_manager_);
        pass_builder_.registerCGSCCAnalyses(gscc_analysis_manager_);
        pass_builder_.registerFunctionAnalyses(function_analysis_manager_);
        pass_builder_.registerLoopAnalyses(loop_analysis_manager_);

        pass_builder_.crossRegisterProxies(
            loop_analysis_manager_, function_analysis_manager_, gscc_analysis_manager_, module_analysis_manager_);
    }

    bool IrManipulationTestHelper::fromString(String const& data)
    {
        module_             = llvm::parseIR(llvm::MemoryBufferRef(data, "IrManipulationTestHelper"), error_, context_);
        compilation_failed_ = (module_ == nullptr);
        return !compilation_failed_;
    }

    String IrManipulationTestHelper::toString() const
    {
        String                   str;
        llvm::raw_string_ostream ostream(str);
        ostream << *module_;
        ostream.flush();
        return str;
    }

    IrManipulationTestHelper::Strings IrManipulationTestHelper::toBodyInstructions()
    {
        if (isModuleBroken())
        {
            return {};
        }

        String  data = toString();
        Strings ret;

        auto pos = data.find("define i8 @Main() local_unnamed_addr");

        if (pos == String::npos)
        {
            return {};
        }

        // Skipping entry
        pos = data.find("entry:", pos);
        if (pos == String::npos)
        {
            return {};
        }

        auto last_pos = data.find('\n', pos);
        assert(last_pos != String::npos);

        auto next_pos   = data.find('\n', last_pos + 1);
        auto terminator = data.find('}', pos);

        while ((next_pos != String::npos) && (next_pos < terminator))
        {
            auto val = data.substr(last_pos, next_pos - last_pos);
            trim(val);

            if (val != "")
            {
                ret.emplace_back(std::move(val));
            }

            last_pos = next_pos;
            next_pos = data.find('\n', last_pos + 1);
        }

        return ret;
    }

    bool IrManipulationTestHelper::hasInstructionSequence(Strings const& instructions)
    {
        auto     body_instructions = toBodyInstructions();
        uint64_t i                 = 0;
        uint64_t j                 = 0;

        while (i < instructions.size() && j < body_instructions.size())
        {
            auto& a = instructions[i];
            auto& b = body_instructions[j];
            if (a == b)
            {
                ++i;
            }

            ++j;
        }

        if (i < instructions.size())
        {
            return false;
        }

        return true;
    }

    void IrManipulationTestHelper::applyProfile(
        GeneratorPtr const&      generator,
        OptimizationLevel const& optimisation_level,
        bool                     debug)
    {
        auto profile = generator->newProfile("generic", optimisation_level, debug);
        profile.apply(*module_);

        // Verifying that the module is valid
        if (isModuleBroken())
        {
            throw std::runtime_error("Module was broken after applying result");
        }
    }

    void IrManipulationTestHelper::declareOpaque(String const& name)
    {
        opaque_declarations_.insert(name);
    }

    void IrManipulationTestHelper::declareFunction(String const& declaration)
    {
        function_declarations_.insert(declaration);
    }

    String IrManipulationTestHelper::generateScript(String const& body, String const& args) const
    {
        String script = R"script(
; ModuleID = 'IrManipulationTestHelper'
source_filename = "IrManipulationTestHelper.ll"

)script";

        // Adding opaque types
        for (auto const& op : opaque_declarations_)
        {
            script += "%" + op + " = type opaque\n";
        }

        script += "define i8 @Main(" + args + ") local_unnamed_addr #0 {\nentry:\n";
        script += body;
        script += "\n  ret i8 0\n";
        script += "\n}\n\n";

        for (auto const& op : function_declarations_)
        {
            script += "declare " + op + "\n";
        }
        script += "\nattributes #0 = { \"EntryPoint\" }\n";
        return script;
    }

    String IrManipulationTestHelper::getErrorMessage() const
    {
        String                   str;
        llvm::raw_string_ostream ostream(str);
        switch (error_.getKind())
        {
        case llvm::SourceMgr::DiagKind::DK_Error:
            ostream << "Error at ";
            break;
        case llvm::SourceMgr::DiagKind::DK_Warning:
            ostream << "Warning at ";
            break;
        case llvm::SourceMgr::DiagKind::DK_Remark:
            ostream << "Remark at ";
            break;
        case llvm::SourceMgr::DiagKind::DK_Note:
            ostream << "Note at ";
            break;
        }

        ostream << error_.getLineNo() << ":" << error_.getColumnNo() << ": ";
        ostream << error_.getMessage() << "\n\n";
        ostream << error_.getLineContents();
        ostream.flush();

        return str;
    }

    bool IrManipulationTestHelper::fromBodyString(String const& body, String const& args)
    {
        auto script = generateScript(body, args);
        return fromString(script);
    }

    IrManipulationTestHelper::ModulePtr& IrManipulationTestHelper::module()
    {
        return module_;
    }

    bool IrManipulationTestHelper::isModuleBroken()
    {
        if (compilation_failed_)
        {
            return compilation_failed_;
        }

        llvm::VerifierAnalysis verifier;
        auto                   result = verifier.run(*module_, module_analysis_manager_);
        return result.IRBroken;
    }

} // namespace quantum
} // namespace microsoft
