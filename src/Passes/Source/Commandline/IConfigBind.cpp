// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Commandline/IConfigBind.hpp"

using namespace microsoft::quantum;

namespace microsoft
{
namespace quantum
{

    IConfigBind::IConfigBind(String const& name, String const& description)
      : name_{name}
      , description_{description}
    {
    }

    IConfigBind::~IConfigBind() = default;

    String IConfigBind::name() const
    {
        return name_;
    }

    String IConfigBind::description() const
    {
        return description_;
    }

    void IConfigBind::setName(String const& name)
    {
        name_ = name;
    }

    bool IConfigBind::isFlag() const
    {
        return is_flag_;
    }

    String IConfigBind::defaultValue() const
    {
        return str_default_value_;
    }

    void IConfigBind::markAsFlag()
    {
        is_flag_ = true;
    }

    void IConfigBind::setDefault(String const& v)
    {
        str_default_value_ = v;
    }

} // namespace quantum
} // namespace microsoft
