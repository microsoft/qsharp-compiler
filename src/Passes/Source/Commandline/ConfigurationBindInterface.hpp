#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ParameterParser.hpp"

#include "Llvm/Llvm.hpp"

#include <iomanip>
#include <iostream>
#include <sstream>
#include <type_traits>
#include <typeindex>

namespace microsoft
{
namespace quantum
{

    /// Interface class to bind a variable to a configuration flag. This class provides
    /// the necessary means to
    class IConfigBind
    {
      public:
        using String = std::string;

        // Deteled constructors, deleted operators and destructor
        //
        IConfigBind(IConfigBind const&) = delete;
        IConfigBind(IConfigBind&&)      = delete;
        IConfigBind& operator=(IConfigBind const&) = delete;
        IConfigBind& operator=(IConfigBind&&) = delete;
        virtual ~IConfigBind();

        // Interface
        //

        /// Interface function to register configuration in the parser. This function
        /// register the configuration to the parameter parser. This makes the configuration
        /// available in the parameter parsers help function.
        virtual bool setupArguments(ParameterParser& parser) = 0;

        /// Interface function to extract configuration. Given an instance of the
        /// parameter parser, the this function is meant to extract and update the
        /// bound variable value if present.
        virtual bool configure(ParameterParser const& parser) = 0;

        /// Interface function to return a string representation of the current value of the
        /// bound variable.
        virtual String value() = 0;

        // Properties
        //

        /// Returns the name of the bound configuration variable.
        String name() const;

        /// Returns the description of the configuration variable.
        String description() const;

        /// Indicates whether or not this
        bool isFlag() const;

        /// Returns the default value for the flag.
        String defaultValue() const;

      protected:
        // Constructor
        //
        IConfigBind(String const& name, String const& description);

        // Configuration
        //

        /// Sets the name of the configuration variable.
        void setName(String const& name);

        /// Marks the variable as a flag.
        void markAsFlag();

        /// Sets the default value as a string.
        void setDefault(String const& v);

      private:
        String name_;              ///< Name that which sets the value.
        String description_;       ///< Description of the option or flag.
        bool   is_flag_{false};    ///< Whether or not the variable is a flag.
        String str_default_value_; ///< Default value represented as a string.
    };

} // namespace quantum
} // namespace microsoft
