#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Logging/ILogger.hpp"

#include <vector>

namespace microsoft
{
namespace quantum
{

    /// Concrete ILogger implementation that prints all messages as IR comments to llvm::errs().
    class CommentLogger : public ILogger
    {
      public:
        // Interface implementation
        //

        /// Adds a debug message to the list.
        void debug(String const& message) override;

        /// Adds an info message to the list.
        void info(String const& message) override;

        /// Adds a warning message to the list.
        void warning(String const& message) override;

        /// Adds an error message to the list.
        void error(String const& message) override;

        /// Adds an internal error message to the list.
        void internalError(String const& message) override;

        /// Function that allows to set the current location.
        void setLocation(String const& name, uint64_t row, uint64_t col) override;

      private:
        String   location_name_{""};
        uint64_t location_row_{0};
        uint64_t location_col_{0};
    };

} // namespace quantum
} // namespace microsoft
