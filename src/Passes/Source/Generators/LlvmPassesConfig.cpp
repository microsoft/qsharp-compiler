// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/LlvmPassesConfig.hpp"

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {

LlvmPassesConfiguration::LlvmPassesConfiguration()
{
  // See :
  // https://opensource.apple.com/source/clang/clang-703.0.29/src/tools/opt/NewPMDriver.cpp.auto.html
  // TODO(tfr): Format here is wrong
  /*
  pass_pipeline_ =
      "targetlibinfo<>,tti<>,targetpassconfig<>,tbaa<>,scoped-noalias<>,assumption-cache-"
      "tracker<>,profile-summary-info<>,forceattrs<>,inferattrs<>,ipsccp<>,called-value-"
      "propagation<>,attributor<>,globalopt<>,domtree<>,mem2reg<>,deadargelim<>,domtree<>,"
      "basicaa<>,aa<>,loops<>,lazy-branch-prob<>,lazy-block-freq<>,opt-remark-emitter<>,"
      "instcombine<>,simplifycfg<>,basiccg<>,globals-aa<>,prune-eh<>,always-inline<>,"
      "functionattrs<>,domtree<>,sroa<>,basicaa<>,aa<>,memoryssa<>,early-cse-memssa<>,"
      "simplifycfg<>,domtree<>,basicaa<>,aa<>,loops<>,lazy-branch-prob<>,lazy-block-freq<>,"
      "opt-remark-emitter<>,instcombine<>,libcalls-shrinkwrap<>,loops<>,branch-prob<>,block-"
      "freq<>,lazy-branch-prob<>,lazy-block-freq<>,opt-remark-emitter<>,pgo-memop-opt<>,"
      "simplifycfg<>,reassociate<>,domtree<>,loops<>,loop-simplify<>,lcssa-verification<>,"
      "lcssa<>,basicaa<>,aa<>,scalar-evolution<>,loop-rotate<>,memoryssa<>,licm<>,loop-"
      "unswitch<>,simplifycfg<>,domtree<>,basicaa<>,aa<>,loops<>,lazy-branch-prob<>,lazy-"
      "block-freq<>,opt-remark-emitter<>,instcombine<>,loop-simplify<>,lcssa-verification<>,"
      "lcssa<>,scalar-evolution<>,indvars<>,loop-idiom<>,loop-deletion<>,loop-unroll<>,phi-"
      "values<>,memdep<>,memcpyopt<>,sccp<>,demanded-bits<>,bdce<>,basicaa<>,aa<>,lazy-"
      "branch-prob<>,lazy-block-freq<>,opt-remark-emitter<>,instcombine<>,postdomtree<>,adce<"
      ">,simplifycfg<>,domtree<>,basicaa<>,aa<>,loops<>,lazy-branch-prob<>,lazy-block-freq<>,"
      "opt-remark-emitter<>,instcombine<>,barrier<>,basiccg<>,rpo-functionattrs<>,globalopt<>"
      ",globaldce<>,basiccg<>,globals-aa<>,domtree<>,float2int<>,lower-constant-intrinsics<>,"
      "domtree<>,loops<>,loop-simplify<>,lcssa-verification<>,lcssa<>,basicaa<>,aa<>,scalar-"
      "evolution<>,loop-rotate<>,loop-accesses<>,lazy-branch-prob<>,lazy-block-freq<>,opt-"
      "remark-emitter<>,loop-distribute<>,branch-prob<>,block-freq<>,scalar-evolution<>,"
      "basicaa<>,aa<>,loop-accesses<>,demanded-bits<>,lazy-branch-prob<>,lazy-block-freq<>,"
      "opt-remark-emitter<>,loop-vectorize<>,loop-simplify<>,scalar-evolution<>,aa<>,loop-"
      "accesses<>,lazy-branch-prob<>,lazy-block-freq<>,loop-load-elim<>,basicaa<>,aa<>,lazy-"
      "branch-prob<>,lazy-block-freq<>,opt-remark-emitter<>,instcombine<>,simplifycfg<>,"
      "domtree<>,basicaa<>,aa<>,loops<>,lazy-branch-prob<>,lazy-block-freq<>,opt-remark-"
      "emitter<>,instcombine<>,loop-simplify<>,lcssa-verification<>,lcssa<>,scalar-evolution<"
      ">,loop-unroll<>,lazy-branch-prob<>,lazy-block-freq<>,opt-remark-emitter<>,instcombine<"
      ">,memoryssa<>,loop-simplify<>,lcssa-verification<>,lcssa<>,scalar-evolution<>,licm<>,"
      "lazy-branch-prob<>,lazy-block-freq<>,opt-remark-emitter<>,transform-warning<>,"
      "alignment-from-assumptions<>,strip-dead-prototypes<>,domtree<>,loops<>,branch-prob<>,"
      "block-freq<>,loop-simplify<>,lcssa-verification<>,lcssa<>,basicaa<>,aa<>,scalar-"
      "evolution<>,block-freq<>,loop-sink<>,lazy-branch-prob<>,lazy-block-freq<>,opt-remark-"
      "emitter<>,instsimplify<>,div-rem-pairs<>,simplifycfg<>,verify<>,write-bitcode<>";
      */
}

void LlvmPassesConfiguration::setup(ConfigurationManager &config)
{
  config.setSectionName("LLVM Passes", "Configuration of LLVM passes.");
  config.addParameter(always_inline_, "always-inline", "Aggressively inline function calls.");
  config.addParameter(pass_pipeline_, "passes",
                      "LLVM passes pipeline to use upon applying this component.");
}

LlvmPassesConfiguration LlvmPassesConfiguration::disable()
{
  LlvmPassesConfiguration ret;
  ret.always_inline_ = false;
  ret.pass_pipeline_ = "";
  return ret;
}

bool LlvmPassesConfiguration::alwaysInline() const
{
  return always_inline_;
}

std::string LlvmPassesConfiguration::passPipeline() const
{
  return pass_pipeline_;
}

bool LlvmPassesConfiguration::isDisabled() const
{
  return always_inline_ == false && pass_pipeline_ == "";
}

bool LlvmPassesConfiguration::isDefault() const
{
  LlvmPassesConfiguration ref{};
  return always_inline_ == ref.always_inline_;
}

}  // namespace quantum
}  // namespace microsoft
