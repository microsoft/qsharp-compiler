// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/IProfileGenerator.hpp"

namespace microsoft {
namespace quantum {

IProfileGenerator::IProfileGenerator() = default;
IProfileGenerator::IProfileGenerator(ConfigurationManager *configuration)
  : configuration_{configuration}
{}
IProfileGenerator::~IProfileGenerator() = default;

}  // namespace quantum
}  // namespace microsoft
