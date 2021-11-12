#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigBind.hpp"
#include "Commandline/IConfigBind.hpp"
#include "Commandline/ParameterParser.hpp"
#include "QatTypes/QatTypes.hpp"

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

    /// ConfigurationManager is a class that holds a collection of configurations (sections). Each of
    /// these sections are embodied in their own class with a one-to-one mapping between configuration
    /// section and the configuration type. As an example, if one wishes to make a configuration for the
    /// class Foo, one would create a class FooConfig which would hold all the variables that are
    /// configurable and then add FooConfig to the ConfigurationManager using `addConfig()`. For
    /// FooConfig to fulfill the concept of a configuration, it must implement a setup functions whose
    /// first argument is the ConfigurationManager.
    class ConfigurationManager
    {
      public:
        using IConfigBindPtr = std::shared_ptr<IConfigBind>; ///< Pointer class used to bind a parameter to a value.
        using ConfigList     = std::vector<IConfigBindPtr>;  ///< List of bound variables.
        using VoidPtr        = std::shared_ptr<void>;        ///< Type-erased configuration pointer.
        using TypeId         = std::type_index;              ///< Type index class.
        using BoolPtr        = std::shared_ptr<bool>;

        /// Section defines a section in the configuration. It holds the type of the configuration class,
        /// the name of the section a description, the instance of the configuration class itself and list
        /// of parameter bindings.
        struct Section
        {
            TypeId     type{TypeId(typeid(std::nullptr_t))}; ///< Type of the configuration.
            String     name{};                               ///< Name of the section.
            String     description{};                        ///< Description of the section.
            VoidPtr    configuration{};                      ///< Configuration class instance.
            ConfigList settings{};                           ///< List of parameter bindings.
            BoolPtr    active{nullptr};                      ///< Whether or not this component is active;
            String     id{};                                 ///< Id referring to this component.
        };
        using Sections = std::vector<Section>; ///< List of available sections

        // Constructors, copy and move operators, destructor
        //

        /// Configuration manager is default constructible, non-copyable and non-movable.
        ConfigurationManager()                            = default;
        ConfigurationManager(ConfigurationManager const&) = delete;
        ConfigurationManager(ConfigurationManager&&)      = delete;
        ConfigurationManager& operator=(ConfigurationManager const&) = delete;
        ConfigurationManager& operator=(ConfigurationManager&&) = delete;

        // Configuration setup
        //

        /// Adds all bound variables as parser arguments.
        void setupArguments(ParameterParser& parser);

        /// Configures the value of each bound variable given a parser instance.
        void configure(ParameterParser const& parser);

        // Managing configuration
        //

        /// Given an instance of the ConfigurationManager, this method override settings of class T.
        template <typename T> inline void setConfig(T const& value);

        /// Gets the configuration instance of type T.
        template <typename T> inline T const& get() const;

        // Support functions
        //

        /// Prints options for configurability to the terminal.
        void printHelp() const;

        /// Prints the configuration to the terminal. The configuration print is LLVM IR compatible
        /// meaning that every line starts with a semicolon ; to ensure that it is interpreted as a
        /// comment.
        void printConfiguration() const;

        // Configuration functions
        //

        /// Adds a new configuration of type T.
        template <typename T> inline void addConfig(String const& id = "", T const& default_value = T());

        /// Whether or not the component associated with T is active.
        template <typename T> inline bool isActive();

        /// Sets the section name. This method is used by the configuration class to set a section
        /// name.
        void setSectionName(String const& name, String const& description);

        /// Adds a new parameter with a default value to the configuration section. This function should
        /// be used by the configuration class.
        template <typename T>
        inline void addParameter(T& bind, T default_value, String const& name, String const& description);

        /// Adds a new parameter to the configuration section. This method uses the bound variable value
        /// as default value. This function should be used by the configuration class.
        template <typename T> inline void addParameter(T& bind, String const& name, String const& description);

      private:
        /// Helper function to get a reference to the configuration of type T.
        template <typename T> inline T& getInternal() const;

        Sections config_sections_{}; ///< All available sections within the ConfigurationManager instance
    };

    template <typename T> inline void ConfigurationManager::addConfig(String const& id, T const& default_value)
    {
        Section new_section{std::type_index(typeid(T))};

        auto ptr                  = std::make_shared<T>(default_value);
        new_section.configuration = ptr;
        new_section.active        = std::make_shared<bool>(true);
        new_section.id            = id;

        config_sections_.emplace_back(std::move(new_section));
        ptr->setup(*this);
    }

    template <typename T> inline T& ConfigurationManager::getInternal() const
    {
        VoidPtr ptr{nullptr};
        auto    type = std::type_index(typeid(T));

        for (auto& section : config_sections_)
        {
            if (section.type == type)
            {
                ptr = section.configuration;
                break;
            }
        }

        if (ptr == nullptr)
        {
            throw std::runtime_error("Could not find configuration class.");
        }

        return *static_cast<T*>(ptr.get());
    }

    template <typename T> inline void ConfigurationManager::setConfig(T const& value)
    {
        auto& config = getInternal<T>();
        config       = value;
    }

    template <typename T> inline T const& ConfigurationManager::get() const
    {
        return getInternal<T>();
    }

    template <typename T> inline bool ConfigurationManager::isActive()
    {
        BoolPtr ptr{nullptr};
        auto    type = std::type_index(typeid(T));

        for (auto& section : config_sections_)
        {
            if (section.type == type)
            {
                ptr = section.active;
                break;
            }
        }

        if (ptr == nullptr)
        {
            throw std::runtime_error("Could not find configuration class.");
        }

        return *ptr;
    }

    template <typename T>
    inline void ConfigurationManager::addParameter(
        T&            bind,
        T             default_value,
        String const& name,
        String const& description)
    {
        auto ptr = std::make_shared<ConfigBind<T>>(bind, default_value, name, description);
        config_sections_.back().settings.push_back(ptr);
    }

    template <typename T>
    inline void ConfigurationManager::addParameter(T& bind, String const& name, String const& description)
    {
        auto ptr = std::make_shared<ConfigBind<T>>(bind, T(bind), name, description);
        config_sections_.back().settings.push_back(ptr);
    }
} // namespace quantum
} // namespace microsoft
