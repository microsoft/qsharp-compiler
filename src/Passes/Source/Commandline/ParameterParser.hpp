#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/Settings.hpp"

#include <string>
#include <unordered_map>
#include <unordered_set>
#include <vector>

namespace microsoft
{
namespace quantum
{

    class ParameterParser
    {
      public:
        using String    = std::string;
        using Arguments = std::vector<String>;
        using Flags     = std::unordered_set<String>;

        /// Construction and deconstrution configuration
        /// @{
        /// Parameter parsers requires a setting class to store
        /// parameters passed. The parameter parser takes a set of
        /// default Settings as its first argument.
        explicit ParameterParser(Settings& settings);

        // No default construction.
        ParameterParser() = delete;

        // No copy construction.
        ParameterParser(ParameterParser const& other) = delete;

        // Allow move semantics.
        ParameterParser(ParameterParser&& other) = default;

        // Default destruction.
        ~ParameterParser() = default;
        /// @}

        /// Configuration
        /// @{

        /// Marks a name as a flag (as opposed to an option).
        /// This ensures that no parameter is expected after
        /// the flag is specified. For instance `--debug` is
        /// a flag as opposed to `--log-level 3` which is an
        /// option.
        void addFlag(String const& v);
        /// @}

        /// Operation
        /// @{
        /// Parses the command line arguments given the argc and argv
        /// from the main function.
        void parseArgs(int argc, char** argv);

        /// Returns list of arguments without flags and/or options
        /// included.
        Arguments const& arguments() const;

        /// Returns the n'th commandline argument.
        String const& getArg(uint64_t const& n);
        /// @}
      private:
        struct ParsedValue
        {
            bool   is_key{false};
            String value;
        };

        /// Helper functions and variables
        /// @{

        // Parses a single argument and returns the parsed value. This function
        // determines if the string was specified to be a key or a value.
        ParsedValue parseSingleArg(String key);

        /// Checks whether a key is an option (or a flag). Returns true if it is
        /// and option and false if it is a flags.
        bool  isOption(String const& key);
        Flags flags_{}; ///< Set of flags
        /// @}

        /// Storage of parsed data
        /// @{
        Settings& settings_;    ///< Map of settings
        Arguments arguments_{}; ///< List of remaining arguments
                                /// @}
    };

} // namespace quantum
} // namespace microsoft
