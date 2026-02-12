# XenoAtom.CommandLine — User Guide

Welcome to the XenoAtom.CommandLine documentation. This guide covers everything from installation to advanced usage.

## Overview

**XenoAtom.CommandLine** is a lightweight, powerful and NativeAOT-friendly command-line parser for .NET. It uses a composition-first API — you declare commands, options, and arguments with collection initializers, and the library handles parsing, help generation, and error reporting.

```csharp
var app = new CommandApp("myexe")
{
    { "n|name=", "Your {NAME}", v => name = v },
    new HelpOption(),
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Hello, {name}!");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
```

## Guides

| Guide | What you'll learn |
|---|---|
| [Getting Started](getting-started.md) | Install the package, create your first app, add options and sub-commands |
| [Options](options.md) | Option prototypes, value types (required/optional/flag), aliases, bundling, key/value pairs, typed parsing, environment variable fallbacks |
| [Commands](commands.md) | Commands and sub-commands, actions, conditional `CommandGroup`, built-in `HelpOption` and `VersionOption`, parsing flow |
| [Arguments](arguments.md) | Positional arguments, cardinality (`<arg>`, `<arg>?`, `<arg>*`, `<arg>+`, `<>`), typed arguments, strict parsing |
| [Validation & Constraints](validation.md) | Value validation (`Validate.Range`, `Validate.NonEmpty`, `Validate.FileExists`, …), mutually exclusive options, requires constraints |
| [Help & Output](help-output.md) | Help text, `CommandUsage`, custom `ICommandOutput` rendering, Terminal markup and visual output |
| [Advanced Topics](advanced.md) | Parse API for testing, shell completions, response files, `CommandConfig`, `CommandRunConfig`, localization, `EnumWrapper<T>`, NativeAOT, performance |

## Quick Links

- [NuGet Package](https://www.nuget.org/packages/XenoAtom.CommandLine/)
- [Source Code](https://github.com/XenoAtom/XenoAtom.CommandLine)
- [Samples](https://github.com/XenoAtom/XenoAtom.CommandLine/tree/main/samples)
- [License](https://github.com/XenoAtom/XenoAtom.CommandLine/blob/main/license.txt)

## Class Diagram

![Class diagram](XenoAtom.CommandLine.png)
