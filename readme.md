# XenoAtom.CommandLine [![ci](https://github.com/XenoAtom/XenoAtom.CommandLine/actions/workflows/ci.yml/badge.svg)](https://github.com/XenoAtom/XenoAtom.CommandLine/actions/workflows/ci.yml) ![coverage](https://gist.githubusercontent.com/xoofx/4b1dc8d0fa14dd6a3846e78e5f0eafae/raw/dotnet-releaser-coverage-badge-XenoAtom-XenoAtom.CommandLine.svg)  [![NuGet](https://img.shields.io/nuget/v/XenoAtom.CommandLine.svg)](https://www.nuget.org/packages/XenoAtom.CommandLine/)

<img align="right" width="256px" height="256px" src="https://raw.githubusercontent.com/XenoAtom/XenoAtom.CommandLine/main/img/icon.png">

**XenoAtom.CommandLine** is a lightweight, powerful and NativeAOT friendly command line parser for .NET

It is a fork of the excellent [NDesk.Options](http://www.ndesk.org/Options)/[Mono.Options](https://tirania.org/blog/archive/2008/Oct-14.html) with significant improvements and new features.

## ✨ Features 

- **Lightweight and NativeAOT-friendly** (`net8.0`+), with **zero dependencies**
- **Composition-first API:** declare commands/options with collection initializers (**no attributes, no base classes, no required “command classes”**)
- **Auto-generated usage/help:** “what you declare is what you get”
- **Commands and sub-commands** (e.g. `git commit -m "message"`)
- **Strict positional arguments by default** (named args + remainder): `<arg>`, `<arg>?`, `<arg>*`, `<arg>+`, `<>`
- **Fast parsing:** optimized hot paths (**no regex**), **low GC allocations**
- **Powerful option parsing**
  - **Prefixes:** `-`, `--`, `/` (e.g. `-v`, `--verbose`, `/v`)
  - **Aliases:** `-v`, `--verbose`
  - **Bundled short options:** `-abc` == `-a -b -c` (tar/POSIX style)
  - **Values:** required `=` / optional `:` (e.g. `-o`, `-oVALUE`, `-o:VALUE`, `-o=VALUE`)
  - **Multiple values:** `-i foo -i bar`
  - **Key/value pairs:** `-DMACRO=VALUE`
- **Built-ins:** `--help` and `--version`
- **Pluggable output rendering:** replace built-in help/error/version/license rendering via `CommandConfig.OutputFactory`
- **Better errors by default**
  - **Strict unknown `-` / `--` options** (`CommandConfig.StrictOptionParsing`)
  - **Helpful diagnostics:** suggestions + “inactive in this context” hints
  - Use `--` to pass values starting with `-` (e.g. `myexe -- -5`); `/mnt/home` is treated as a positional value (not an option)
- **Response files:** `@file.txt` (supports quotes, `#` comments, and basic escaping on non-Windows)
- **Conditional groups:** declare commands/options that are only active when a condition is met
- **Shell completions:** bash/zsh/fish/PowerShell via `CompletionCommands`, token protocol, optional value completions (`ValueCompleter`)

## 🧪 Example

```csharp
using System;
using XenoAtom.CommandLine;

const string _ = "";
string? name = null;
int age = 0;
List<(string, string?)> keyValues = new List<(string, string?)>();
List<string> messages = new List<string>();
List<string> commitFiles = new List<string>();

var commandApp = new CommandApp("myexe")
{
    new CommandUsage(),
    _,
    {"D:", "Defines a {0:name} and optional {1:value}", (key, value) =>
    {
        if (key is null) throw new OptionException("The key is mandatory for a define", "D");
        keyValues.Add((key, value));
    }},
    {"n|name=", "Your {NAME}", v => name = v},
    {"a|age=", "Your {AGE}", (int v) => age = v},
    new HelpOption(),
    _,
    "Available commands:",
    new Command("commit")
    {
        _,
        {"m|message=", "Add a {MESSAGE} to this commit", messages},
        _,
        "Arguments:",
        { "<files>*", "Files to commit", commitFiles },
        new HelpOption(),

        // Action for the commit command
        (ctx, _) =>
        {
            ctx.Out.WriteLine($"Committing with name={name}, age={age}");
            foreach (var message in messages)
            {
                ctx.Out.WriteLine($"Commit message: {message}");
            }
            foreach (var file in commitFiles)
            {
                ctx.Out.WriteLine($"Commit file: {file}");
            }
            return ValueTask.FromResult(0);
        }
    },
    // Default action if no command is specified
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Hello {name}! You are {age} years old.");
        if (keyValues.Count > 0)
        {
            foreach (var keyValue in keyValues)
            {
                ctx.Out.WriteLine($"Define: {keyValue.Item1} => {keyValue.Item2}");
            }
        }

        return ValueTask.FromResult(0);
    }
};

await commandApp.RunAsync(args);
```

Notes:
- `CommandUsage()` defaults to `Usage: {NAME} {SYNTAX}` and `{SYNTAX}` is derived from your declared options/commands/arguments.
- Positional arguments are strict by default: declare `<arg>` / `<arg>?` / `<arg>*` / `<arg>+`, or declare `<>` to forward remaining arguments to the command action.

Running `myexe --help` will output:

```
Usage: myexe [options] <command>

  -D[=name:value]            Defines a name and optional value
  -n, --name=NAME            Your NAME
  -a, --age=AGE              Your AGE
  -h, -?, --help             Show this message and exit

Available commands:
  commit
```

Running `myexe --name John -a50` will output:

```
Hello John! You are 50 years old.
```

Running `myexe --name John -a50 -DHello -DWorld=121` will output:

```
Hello John! You are 50 years old.
Define: Hello =>
Define: World => 121
```

Running `myexe commit --help` will output:

```
Usage: myexe commit [options] <files>*

  -m, --message=MESSAGE      Add a MESSAGE to this commit
  -h, -?, --help             Show this message and exit

Arguments:
  <files>*                   Files to commit
```

Running `myexe --name John -a50 commit --message "Hello!" --message "World!"` will output:

```
Committing with name=John, age=50
Commit message: Hello!
Commit message: World!
```

## 📃 User Guide

For more details on how to use XenoAtom.CommandLine, please visit the [user guide](https://github.com/XenoAtom/XenoAtom.CommandLine/blob/main/doc/readme.md).

## 🏗️ Build

You need to install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Then from the root folder:

```console
$ dotnet build src -c Release
```

## 🪪 License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause).

The license also integrate the original MIT license from [Mono.Options](https://github.com/mono/mono/blob/main/mcs/class/Mono.Options/Mono.Options/Options.cs).

## 🤗 Author

Alexandre Mutel aka [xoofx](https://xoofx.github.io).
