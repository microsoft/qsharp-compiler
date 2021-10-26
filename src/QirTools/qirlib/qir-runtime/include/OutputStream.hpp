// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#ifndef OUTPUTSTREAM_HPP
#define OUTPUTSTREAM_HPP

#include <ostream>
#include "CoreTypes.hpp" // QIR_SHARED_API

namespace Microsoft // Replace with `namespace Microsoft::Quantum` after migration to C++17.
{
namespace Quantum
{
    struct QIR_SHARED_API OutputStream
    {
        struct QIR_SHARED_API ScopedRedirector
        {
            explicit ScopedRedirector(std::ostream& newOstream);
            ~ScopedRedirector();

          private:
            ScopedRedirector(const ScopedRedirector&) = delete;
            ScopedRedirector& operator=(const ScopedRedirector&) = delete;
            ScopedRedirector(ScopedRedirector&&)                 = delete;
            ScopedRedirector& operator=(ScopedRedirector&&) = delete;

          private:
            std::ostream& old;
        };

        static std::ostream& Get();
        static std::ostream& Set(std::ostream& newOStream);

      private:
        static std::ostream* currentOutputStream;
    };

} // namespace Quantum
} // namespace Microsoft

#endif // #ifndef OUTPUTSTREAM_HPP
