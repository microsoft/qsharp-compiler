# Microsoft Quantum Development Kit Telemetry Library

## Introduction

The Telemetry library is a .NET class library to make it easier for developers of the Quantum Development Kit to instrument the applications and tools.

But more important than that, this library implements data collection best practices that aligns with [Microsoft Privacy Statement](https://privacy.microsoft.com/privacystatement).

This library provides a single place where we can put all of our core telemetry logic, reducing code duplication across multiple QDK products and bringing the following benefits:

1. Makes it easier to comply to telemetry, data collection and privacy policies in a standard/consistent way, such as opt-in and opt-out controls
2. Comprehensive unit test (and, in the future, integration test) coverage
Ability to share common event properties and schemas across different QDK products
3. Makes it easier for QDK developers to add instrumentation to the products (with included C# and F# samples)
4. Support for out-of-process telemetry upload, to reduce the run time of short-lived console applications

## Target audience

The target audience for this library is any developer of the Quantum Development Kit and, although that includes community contributors, most likely the users of this library will be internal Microsoft developers.

## How to use the Telemetry Library

Please see the [Samples](./Samples/) folder where we include a complete sample app for [C#](./Samples/CSharp/) and [F#](./Samples/CSharp/).

1. From your .NET application, add a project reference to the `/Library/Telemetry.csproj`. We plan to publish this as a internal Nuget package in the future.

```bash
dotnet add reference {path to TelemetryLibrary}/Library/Telemetry.csproj
```

2. Create the configuration object `TelemetryManagerConfig` with your preferred options. See the [source documentation](./Library/TelemetryManagerConfig.cs) for details on each option.

```csharp
var telemetryConfig = new TelemetryManagerConfig()
{
    AppId = "SampleCSharpApp",
    HostingEnvironmentVariableName = "SAMPLECSHARPAPP_HOSTING_ENV",
    TelemetryOptOutVariableName = "QDK_TELEMETRY_OPT_OUT",
    MaxTeardownUploadTime = TimeSpan.FromSeconds(2),
    OutOfProcessUpload = true,
    ExceptionLoggingOptions = new ExceptionLoggingOptions()
    {
        CollectTargetSite = true,
        CollectSanitizedStackTrace = true,
    },
    SendTelemetryInitializedEvent = true,
    SendTelemetryTearDownEvent = true,
    DefaultTelemetryConsent = ConsentKind.OptedIn,
};
```

3. In your entrypoint method, initialize the `TelemetryManager`, properly handling its IDisposable result.

```csharp
using (TelemetryManager.Initialize(telemetryConfig))
{
    // your program code here
}
```

4. Use the `TelemetryManager` static methods to log objects and exceptions.

```csharp
MyEvent myEvent = new()
    {
        MyEventProperty = "sample string",
    };
TelemetryManager.LogObject(myEvent);

try
{
    // your code here
}
catch (Exception exception)
{
    TelemetryManager.LogObject(exception);
}
```

## Important Config options and Environment Variables

### .AppId

The AppId is the key value that identifies all the telemetry coming from your application in the backend systems.

It's also used to prefix all event names as follows:

```csharp
$"{Configuration.EventNamePrefix}_{Configuration.AppId}_"
```

**Make sure to pass your AppId in the config:**

```csharp
var telemetryConfig = new TelemetryManagerConfig()
{
    AppId = "SampleCSharpApp",
}
```

**Note:** The AppId must have from 3 to 20 characters and only letters and numbers are allowed.

### .DefaultTelemetryConsent

The default User Consent to collect or not collect telemetry (defaults to ConsentKind.OptedOut, can also be set ConsentKind.OptedIn).
If the default value is ConsentKind.OptedOut, then the user can opt-in via the environment variable defined in TelemetryOptInVariableName.
If the default value is ConsentKind.OptedIn, then the user can opt-out via the environment variable defined in TelemetryOptOutVariableName.

### .TelemetryOptInVariableName

This is the name of the environment variable that the Telemetry Library will use to determine if the end user has opted-in sending telemetry.

For example, if your `TelemetryOptInVariableName` is `MYAPP_TELEMETRY_OPT_IN`, and the end user sets it to `MYAPP_TELEMETRY_OPT_IN="1"`, data will be collected and sent to Microsoft.

**Note:** The "TelemetryOptOut" will always take precedence.

### .TelemetryOptOutVariableName

This is the name of the environment variable that the Telemetry Library will use to determine if the end user has opted-out of sending telemetry.

For example, if your `TelemetryOptOutVariableName` is `MYAPP_TELEMETRY_OPT_OUT`, and the end user sets it to `MYAPP_TELEMETRY_OPT_OUT="1"`, then no data will be send to Microsoft.

### .HostingEnvironmentVariableName

This is the name of the environment variable that the Telemetry Library will use to send the common `HostingEnvironment` property.

For example, if your `HostingEnvironmentVariableName` is `MYAPP_HOSTING_ENV`, and the end user sets it to `MYAPP_HOSTING_ENV="test-agent"`, then all events will have a property `"HostingEnvironment"="test-agent"`.

This is particularly useful for us to identify the environment in which the application is running, like automated build or test agents, and filter them out from our usage reports.

### .EnableTelemetryExceptionsVariableName

This is the name of the environment variable that the Telemetry Library will use to determine whether or not to override the option to allow the Telemetry Library to raise or rethrow unhandled telemetry exceptions.
See [Telemetry Library Exceptions](#Telemetry-Library-Exceptions) for more info.

For example, if your `EnableTelemetryExceptionsVariableName` is `ENABLE_MYAPP_TELEMETRY_EXCEPTIONS`, and the end user sets it to `ENABLE_MYAPP_TELEMETRY_EXCEPTIONS="1"`, then the Telemetry Library is allowed to throw exceptions and therefore potentially impacting the user experience.

We should always have this enabled in CI tests to make sure we detect unexpected problems with the Telemetry Library.

### .EnableTelemetryTestVariableName

This is the name of the environment variable that the Telemetry Library will use enable the [Test Mode](#.TestMode).

For example, if your `EnableTelemetryTestVariableName` is `ENABLE_MYAPP_TELEMETRY_TEST`, and the end user sets it to `ENABLE_MYAPP_TELEMETRY_TEST="1"`, then all events will be printed to the Debug console instead being sent to Microsoft.

### .TestMode

If `true` all events will be printed to the Debug console instead being sent to Microsoft.

### .ExceptionLoggingOptions

The `ExceptionLoggingOptions` provides finer control on how the Telemetry Library logs `Exceptions`.

By default, the Telemetry Library only logs the full name of the exception, to prevent user content or sensitive information to be send to Microsoft.

The app developer can also enable the inclusion of other Exception properties that would not contain user content or sensitive information.

#### .ExceptionLoggingOptions.CollectTargetSite

Logs the full method name where the exception was caught.

#### .ExceptionLoggingOptions.CollectSanitizedStackTrace

Logs the stack trace containing the full method names from where the exception was thrown to where the exception was caught, removing all other potentially sensitive information like file paths.

## Telemetry Library Exceptions

As any piece of code, the Telemetry Library may "find itself in trouble" and raise unexpected exceptions.

To avoid the telemetry to affect the end user experience, the Telemetry Library will by default suppress all unhandled exceptions thrown by its internal stack.

There are a few ways to change that default behavior:

1. When either the `DEBUG` or `ENABLE_QDK_TELEMETRY_EXCEPTIONS` are present in the Compiler directives, the Telemetry Library will not suppress exceptions.

2. When the [.EnableTelemetryExceptionsVariableName](#.EnableTelemetryExceptionsVariableName) environment variable is used, it will override the default behavior or the compiler directives above.

## Effect of Compiler Directives

The following compiler directives can change the behavior of the Telemetry Library:

- `DEBUG`: When present, the Telemetry Library will print some debug/trace information into the Debug Console. It will also enable [Telemetry Exceptions](#Telemetry-Library-Exceptions).

- `ENABLE_QDK_TELEMETRY_EXCEPTIONS`: See [Telemetry Exceptions](#Telemetry-Library-Exceptions).

## Dependencies

The Telemetry Library is a convenient wrapper around the internal `Microsoft.Applications.Events.Client` instrumentation library (aka Aria).

It depends on the `Microsoft.Applications.Events.Client` Nuget package that is only available in a private Nuget channel `https://msasg.pkgs.visualstudio.com/_packaging/OneSDK/nuget/v3/index.json`, which requires authentication.

We have re-published this package into the QDK Alpha channel `https://pkgs.dev.azure.com/ms-quantum-public/9af4e09e-a436-4aca-9559-2094cfe8d80c/_packaging/alpha/nuget/v3/index.json` to allow this Telemetry Library to be compiled by anyone.

### Upgrading the dependency (Microsoft internal developers)

When necessary to upgrade the package to a newer version (the package is super stable), we will need to re-publish it to `QDK Alpha` channel.

Steps:

1. Install the [Credential Provider](https://github.com/microsoft/artifacts-credprovider) to to enable authentication with Nuget

```powershell
iex "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) }"
```

2. Run the following script passing the appropriate package version

```powershell
./Build/update-aria-package.ps1 1.1.1.337
```

## Development

### Restore dependencies

```powershell
dotnet restore
```

### Build

```powershell
dotnet build
```

### Test

```powershell
dotnet test
```

#### Test coverage report

```powershell
# Restore the dotnet-reportgenerator-cli tool
dotnet tool restore

./build/test-coverage.ps1
```

### F# style formatter

1. Install [Fantomas](https://github.com/fsprojects/fantomas):

```powershell
dotnet tool install -g fantomas-tool
dotnet tool restore
```

2. Run Fantomas while developing F# code

```powershell
dotnet fantomas --recurse .
```

### VS Code

Install the following extensions to make your life easier:

- PowerShell
- C#
- Ionide-fsharp (there is a conflict with Shader Language support extension, as it will pick .fs files)
- fantomas-fmt
