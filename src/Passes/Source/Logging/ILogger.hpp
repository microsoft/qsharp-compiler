#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Types/Types.hpp"

#include <cstdint>
#include <string>

namespace microsoft
{
namespace quantum
{

    /// Logger interface to allow the collection of different types of messages during QIR
    /// transformation and/or validation.
    class ILogger
    {
      public:
        // Constructors, copy and move operators and destructors
        //

        ILogger()               = default;
        ILogger(ILogger const&) = default;
        ILogger(ILogger&&)      = default;
        ILogger& operator=(ILogger const&) = default;
        ILogger& operator=(ILogger&&) = default;

        virtual ~ILogger() = default;

        // Abstract interface methods
        //

        /// Reports a debug message.
        virtual void debug(String const& message) = 0;

        /// Reports an info message.
        virtual void info(String const& message) = 0;

        /// Reports a warning message.
        virtual void warning(String const& message) = 0;

        /// Reports an error message.
        virtual void error(String const& message) = 0;

        /// Reports an internal error message.
        virtual void internalError(String const& message) = 0;

        /// Sets the current location. Importantly, the location can be set independently of the reported
        /// messages. This allows one to update the location upon updating the cursor position without
        /// having to worry about keeping a copy of the location to pass when reporting messages.
        /// The most obvious case of this is file path (name) with a line and character (row, col).
        virtual void setLocation(String const& name, uint64_t row, uint64_t col) = 0;
    };

} // namespace quantum
} // namespace microsoft
