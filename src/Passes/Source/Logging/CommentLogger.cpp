// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Logging/CommentLogger.hpp"

#include "Llvm/Llvm.hpp"

#include <vector>

namespace microsoft
{
namespace quantum
{

    void CommentLogger::debug(String const& message)
    {
        llvm::errs() << "debug - " << location_name_ << ":" << location_row_ << "," << location_col_ << " - " << message
                     << "\n";
    }

    void CommentLogger::info(String const& message)
    {
        llvm::errs() << "info - " << location_name_ << ":" << location_row_ << "," << location_col_ << " - " << message
                     << "\n";
    }

    void CommentLogger::warning(String const& message)
    {
        llvm::errs() << "warning - " << location_name_ << ":" << location_row_ << "," << location_col_ << " - "
                     << message << "\n";
    }

    void CommentLogger::error(String const& message)
    {
        llvm::errs() << "error - " << location_name_ << ":" << location_row_ << "," << location_col_ << " - " << message
                     << "\n";
    }

    void CommentLogger::internalError(String const& message)
    {
        llvm::errs() << "internal error - " << location_name_ << ":" << location_row_ << "," << location_col_ << " - "
                     << message << "\n";
    }

    void CommentLogger::setLocation(String const& name, uint64_t row, uint64_t col)
    {
        location_name_ = name;
        location_row_  = row;
        location_col_  = col;
    }

} // namespace quantum
} // namespace microsoft
