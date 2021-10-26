#ifndef COREDEFINES_H
#define COREDEFINES_H

#ifdef _WIN32
#ifdef EXPORT_QIR_API
#define QIR_SHARED_API __declspec(dllexport)
#else
#define QIR_SHARED_API __declspec(dllimport)
#endif
#else
#define QIR_SHARED_API
#endif

#endif // #ifndef COREDEFINES_H
