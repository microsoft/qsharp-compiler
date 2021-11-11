#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Logging/ILogger.hpp"
#include "Profile/Profile.hpp"
#include "QatTypes/QatTypes.hpp"
#include "ValidationPass/ValidationPassConfiguration.hpp"

#include "Llvm/Llvm.hpp"

#include <functional>
#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    class ValidationPass : public llvm::PassInfoMixin<ValidationPass>
    {
      public:
        using Instruction = llvm::Instruction;
        using Value       = llvm::Value;

        // Construction and destruction configuration.
        //

        explicit ValidationPass(ValidationPassConfiguration const& cfg)
          : config_{cfg}
        {
        }

        /// Copy construction is banned.
        ValidationPass(ValidationPass const&) = delete;

        /// We allow move semantics.
        ValidationPass(ValidationPass&&) = default;

        /// Default destruction.
        ~ValidationPass() = default;

        llvm::PreservedAnalyses run(llvm::Module& module, llvm::ModuleAnalysisManager& mam);
        /// Whether or not this pass is required to run.
        static bool isRequired();

      private:
        ValidationPassConfiguration config_{};

        std::unordered_map<std::string, uint64_t> opcodes_;
        std::unordered_map<std::string, uint64_t> external_calls_;
        std::unordered_map<std::string, uint64_t> internal_calls_;
    };

} // namespace quantum
} // namespace microsoft
