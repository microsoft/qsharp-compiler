#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Types/Types.hpp"

#include "Llvm/Llvm.hpp"

#include <functional>
#include <unordered_set>
#include <vector>

namespace microsoft
{
namespace quantum
{

    class RemoveDisallowedAttribubtesPass : public llvm::PassInfoMixin<RemoveDisallowedAttribubtesPass>
    {
      public:
        RemoveDisallowedAttribubtesPass()
          : allowed_attrs_{{static_cast<String>("EntryPoint"), static_cast<String>("InteropFriendly")}}
        {
        }

        llvm::PreservedAnalyses run(llvm::Module& module, llvm::ModuleAnalysisManager& /*mam*/)
        {
            for (auto& function : module)
            {
                std::unordered_set<String> to_keep;

                // Finding all valid attributes
                for (auto& attrset : function.getAttributes())
                {
                    for (auto& attr : attrset)
                    {
                        auto r = static_cast<String>(attr.getAsString());

                        // Stripping quotes
                        if (r.size() >= 2 && r[0] == '"' && r[r.size() - 1] == '"')
                        {
                            r = r.substr(1, r.size() - 2);
                        }

                        // Inserting if allowed
                        if (allowed_attrs_.find(r) != allowed_attrs_.end())
                        {
                            to_keep.insert(r);
                        }
                    }
                }

                // Deleting every
                function.setAttributes({});
                for (auto& attr : to_keep)
                {
                    function.addFnAttr(attr);
                }
            }

            return llvm::PreservedAnalyses::none();
        }

      private:
        std::unordered_set<String> allowed_attrs_;
    };

} // namespace quantum
} // namespace microsoft
