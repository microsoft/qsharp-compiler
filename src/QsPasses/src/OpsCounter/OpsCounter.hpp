#pragma once
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "Llvm.hpp"

class COpsCounterPrinter : public llvm::PassInfoMixin<COpsCounterPrinter>
{
public:
  explicit COpsCounterPrinter(llvm::raw_ostream &out_stream)
    : out_stream_(out_stream)
  {}

  //  llvm::PreservedAnalyses run(llvm::Function &function, llvm::FunctionAnalysisManager &fam);
  auto run(llvm::Function &f, llvm::FunctionAnalysisManager & /*unused*/)
      -> llvm::PreservedAnalyses  // NOLINT
  {
    out_stream_ << "(operation-counter) " << f.getName() << "\n";
    out_stream_ << "(operation-counter)   number of arguments: " << f.arg_size() << "\n";

    return llvm::PreservedAnalyses::all();
  }
  /*
  TODO(TFR): Documentation suggests that there such be a isRequired, however, comes out as
  unused after compilation
  */

  static bool isRequired()
  {
    return true;
  }

private:
  llvm::raw_ostream &out_stream_;
};

auto GetOpsCounterPluginInfo() -> llvm::PassPluginLibraryInfo;
