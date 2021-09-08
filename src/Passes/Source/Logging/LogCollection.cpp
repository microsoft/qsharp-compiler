// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Logging/LogCollection.hpp"

#include <vector>

namespace microsoft
{
namespace quantum
{

    void LogCollection::debug(String const& message) override
    {
        messages_.emplace_back({Debug, current_location_, message});
    }

    void LogCollection::info(String const& message) override
    {
        messages_.emplace_back({Info, current_location_, message});
    }

    void LogCollection::warning(String const& message) override
    {
        messages_.emplace_back({Warning, current_location_, message});
    }

    void LogCollection::error(String const& message) override
    {
        messages_.emplace_back({Error, current_location_, message});
    }

    void LogCollection::internalError(String const& message) override
    {
        messages_.emplace_back({InternalError, current_location_, message});
    }

    void LogCollection::setLocation(String const& name, uint64_t row, uint64_t col) override
    {
        current_location_.name = name;
        current_location_.row  = row;
        current_location_.col  = col;
    }

} // namespace quantum
} // namespace microsoft
