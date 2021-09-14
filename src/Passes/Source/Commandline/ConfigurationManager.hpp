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

    class IConfigBind
    {
      public:
        using String = std::string;

        IConfigBind(String const& name, String const& description);
        virtual ~IConfigBind();

        virtual bool   setupArguments(ParameterParser& parser)  = 0;
        virtual bool   configure(ParameterParser const& parser) = 0;
        virtual String value()                                  = 0;

        String name() const;
        String description() const;
        void   setName(String const& name);
        bool   isFlag() const;
        String defaultValue() const;

      protected:
        void markAsFlag();
        void setDefault(String const& v);

      private:
        String name_;
        String description_;
        bool   is_flag_{false};
        String str_default_value_;
    };

    template <typename T> class ConfigBind : public IConfigBind
    {
      public:
        using String = std::string;
        using Type   = T;
        template <typename A, typename B, typename R>
        using EnableIf = typename std::enable_if<std::is_same<A, B>::value, R>::type;

        ConfigBind(Type& bind, T default_value, String const& name, String const& description)
          : IConfigBind(name, description)
          , bind_{bind}
          , default_value_{std::move(default_value)}
        {
            alterNameBasedOnType(default_value_);
        }

        template <typename R> void alterNameBasedOnType(R const& default_value)
        {
            std::stringstream ss{""};
            ss << default_value;
            setDefault(ss.str());
        }

        void alterNameBasedOnType(bool const& default_value)
        {
            markAsFlag();

            if (default_value)
            {
                setDefault("true");
            }
            else
            {
                setDefault("false");
            }
        }

        bool setupArguments(ParameterParser& parser) override
        {
            return setupArguments(parser, default_value_);
        }

        template <typename R> bool setupArguments(ParameterParser&, R const&)
        {
            return true;
        }

        bool setupArguments(ParameterParser& parser, bool const&)
        {
            parser.addFlag(name());
            return true;
        }

        bool configure(ParameterParser const& parser) override
        {
            loadValue<Type>(parser, default_value_);
            return true;
        }

        String value() override
        {
            return valueAsString<Type>(default_value_);
        }

        Type& bind()
        {
            return bind_;
        }

      private:
        template <typename A> String valueAsString(A const&)
        {
            std::stringstream ss{""};
            ss << bind_;
            return ss.str();
        }

        template <typename A> String valueAsString(EnableIf<A, bool, A> const&)
        {
            std::stringstream ss{""};
            ss << (bind_ ? "true" : "false");
            return ss.str();
        }

        template <typename R> void loadValue(ParameterParser const& parser, R const& default_value)
        {
            bind_ = default_value;

            if (parser.has(name()))
            {
                std::stringstream ss{parser.get(name())};
                ss >> bind_;
            }
        }

        template <typename A> void loadValue(ParameterParser const& parser, EnableIf<A, bool, A> const& default_value)
        {
            bind_ = default_value;
            if (parser.has(name()))
            {
                bind_ = true;
            }
            else if (parser.has("no-" + name()))
            {
                bind_ = false;
            }
        }

        template <typename A> void loadValue(ParameterParser const& parser, EnableIf<A, String, A> const& default_value)
        {
            bind_ = default_value;

            if (parser.has(name()))
            {
                bind_ = parser.get(name());
            }
        }

        Type& bind_;
        Type  default_value_;
    };

    struct ConfigurationManager
    {
      public:
        using String         = std::string;
        using IConfigBindPtr = std::shared_ptr<IConfigBind>;
        using ConfigList     = std::vector<IConfigBindPtr>;
        using VoidPtr        = std::shared_ptr<void>;
        struct Section
        {
            std::type_index type{std::type_index(typeid(nullptr_t))};
            String          name{};
            String          description{};
            VoidPtr         configuration{};

            ConfigList settings{};
        };
        using Sections = std::vector<Section>;

        ConfigurationManager()                            = default;
        ConfigurationManager(ConfigurationManager const&) = delete;
        ConfigurationManager(ConfigurationManager&&)      = delete;
        ConfigurationManager& operator=(ConfigurationManager const&) = delete;
        ConfigurationManager& operator=(ConfigurationManager&&) = delete;

        void setupArguments(ParameterParser& parser);
        void configure(ParameterParser const& parser);
        void printHelp() const;
        void printConfiguration() const;

        void setSectionName(String const& name, String const& description);

        template <typename T> inline void     addConfig();
        template <typename T> inline void     setConfig(T const& value);
        template <typename T> inline T const& get() const;
        template <typename T>
        inline void addParameter(T& bind, T default_value, String const& name, String const& description);
        template <typename T> inline void addParameter(T& bind, String const& name, String const& description);

      private:
        Sections config_sections_;
    };

    template <typename T> inline void ConfigurationManager::addConfig()
    {
        Section new_section{std::type_index(typeid(T))};

        auto ptr                  = std::make_shared<T>();
        new_section.configuration = ptr;

        config_sections_.emplace_back(std::move(new_section));
        ptr->setup(*this);
    }

    template <typename T> inline void ConfigurationManager::setConfig(T const& value)
    {
        auto    type = std::type_index(typeid(T));
        VoidPtr ptr{nullptr};
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

        auto& config = *static_cast<T*>(ptr.get());
        config       = value;
    }

    template <typename T> inline T const& ConfigurationManager::get() const
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
