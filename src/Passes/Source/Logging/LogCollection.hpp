#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Logging/ILogger.hpp"

#include <vector>

namespace microsoft
{
namespace quantum
{

    /// Concrete ILogger implementation that collects all messages and their corresponding location in a
    /// list that can be traversed later on.
    class LogCollection : public ILogger
    {
      public:
        /// Class that holds the location of where the incident happened.
        struct Location
        {
            String   name{};
            uint64_t row{0};
            uint64_t col{0};
        };

        /// Enum description what type of information we are conveying.
        enum class Type
        {
            Debug,
            Info,
            Warning,
            Error,
            InternalError,
        };

        /// Struct to hold a message together with its type and location
        struct Message
        {
            Type     type;
            Location location;
            String   message;
        };

        /// List of messages defined as alias.
        using Messages = std::vector<Message>;

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
        Location current_location_{}; ///< Holds current location.
        Messages messages_;           ///< All messages emitted.
    };

} // namespace quantum
} // namespace microsoft
