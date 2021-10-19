#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "AllocationManager/IAllocationManager.hpp"
#include "Llvm/Llvm.hpp"
#include "ValueTracker/ValueTracker.hpp"

namespace microsoft {
namespace quantum {

class IProfileGenerator;

/// Profile class that defines a set of rules which constitutes the profile definition. Each of the
/// rules can be used to transform a generic QIR and/or validate that the QIR is compliant with said
/// rule.
class Profile
{
public:
  /// Allocation manager pointer type. Used to reference to concrete allocation manager
  /// implementations which defines the allocation logic of the profile.
  using AllocationManagerPtr = IAllocationManager::AllocationManagerPtr;
  using ValueTrackerPtr      = ValueTracker::ValueTrackerPtr;
  // Constructors
  //

  explicit Profile(
      bool                 debug,
      AllocationManagerPtr qubit_allocation_manager  = BasicAllocationManager::createNew(),
      AllocationManagerPtr result_allocation_manager = BasicAllocationManager::createNew(),
      ValueTrackerPtr      value_tracker             = ValueTracker::createNew());

  // Default construction not allowed as this leads to invalid configuration of the allocation
  // managers.

  Profile()                = delete;
  Profile(Profile const &) = delete;
  Profile(Profile &&)      = default;
  Profile &operator=(Profile const &) = delete;
  Profile &operator=(Profile &&) = default;
  ~Profile()                     = default;

  // Profile methods
  //

  /// Applies the profile to a module.
  void apply(llvm::Module &module);

  /// Verifies that a module is a valid LLVM IR.
  bool verify(llvm::Module &module);

  /// Validates that a module complies with the specified QIR profile.
  bool validate(llvm::Module &module);

  AllocationManagerPtr getQubitAllocationManager();
  AllocationManagerPtr getResultAllocationManager();

  // Access functions to LLVM instances for running the
  // module pass manager
  //

protected:
  // Ensuring that IProfileGenerator has access to following protected functions.
  friend class IProfileGenerator;

  /// Sets the module pass manager used for the transformation of the IR.
  void setModulePassManager(llvm::ModulePassManager &&manager);

  /// Returns a reference to the pass builder.
  llvm::PassBuilder &passBuilder();

  /// Returns a reference to the loop analysis manager.
  llvm::LoopAnalysisManager &loopAnalysisManager();

  /// Returns a reference to the function analysis manager.
  llvm::FunctionAnalysisManager &functionAnalysisManager();

  /// Returns a reference to the GSCC analysis manager.
  llvm::CGSCCAnalysisManager &gsccAnalysisManager();

  /// Returns a reference to the module analysis manager.
  llvm::ModuleAnalysisManager &moduleAnalysisManager();

private:
  // LLVM logic to run the passes
  //
  llvm::PassBuilder             pass_builder_;
  llvm::LoopAnalysisManager     loop_analysis_manager_;
  llvm::FunctionAnalysisManager function_analysis_manager_;
  llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
  llvm::ModuleAnalysisManager   module_analysis_manager_;

  llvm::ModulePassManager module_pass_manager_{};

  // Allocation management
  //

  /// Interface pointer to the qubit allocation manager. Mode of operation depends on the concrete
  /// implementation of the manager which is swappable through the interface.
  AllocationManagerPtr qubit_allocation_manager_{};

  /// Interface pointer to the results allocation manager. Again here the manager behaviour is
  /// determined by its implementation details.
  AllocationManagerPtr result_allocation_manager_{};

  /// Value Tracker
  ValueTrackerPtr value_tracker_{};
};

}  // namespace quantum
}  // namespace microsoft
