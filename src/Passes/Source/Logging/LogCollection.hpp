#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Logging/ILogger.hpp"

#include <vector>

namespace microsoft
{
namespace quantum
{

    class LogCollection : public ILogger
    {
      public:
        struct Location
        {
            String   name{};
            uint64_t row{0};
            uint64_t col{0};
        };
        enum class Type
        {
            Debug,
            Info,
            Warning,
            Error,
            InternalError,
        };

        struct Message
        {
            Type     type;
            Location location;
            String   message;
        };
        using Messages = std::vector<Message>;

        void debug(String const& message) override;
        void info(String const& message) override;
        void warning(String const& message) override;
        void error(String const& message) override;
        void internalError(String const& message) override;
        void setLocation(String const& name, uint64_t row, uint64_t col) override;

      private:
        Location current_location_{};
        Messages messages_;
    };

} // namespace quantum
} // namespace microsoft
