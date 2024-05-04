# XenoAtom.CommandLine [![ci](https://github.com/XenoAtom/XenoAtom.CommandLine/actions/workflows/ci.yml/badge.svg)](https://github.com/XenoAtom/XenoAtom.CommandLine/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/XenoAtom.CommandLine.svg)](https://www.nuget.org/packages/XenoAtom.CommandLine/)

<img align="right" width="256px" height="256px" src="https://raw.githubusercontent.com/XenoAtom/XenoAtom.CommandLine/main/img/icon.png">

**XenoAtom.CommandLine** is a lightweight, powerful and NativeAOT friendly command line parser.

It is a fork of the excellent [NDesk.Options](http://www.ndesk.org/Options)/[Mono.Options](https://tirania.org/blog/archive/2008/Oct-14.html) with several small improvements and new features.

## ✨ Features 

- Lightweight library with no dependencies
- `net8.0`+  ready and NativeAOT friendly, no `System.Reflection` used
- Provides a simple API to parse command line arguments
- Generates a help message from the command line definition
    - What you declare is what you get!
- Supports 
    - Commands and sub-command parsing (e.g. `git commit -m "message"`)
    - Tar and POSIX style options (e.g. `-abc` is equivalent to `-a -b -c`)
    - `-`, `/` and `--` option prefixes (e.g. `-v`, `/v`, `--verbose`))
    - Multiple option values (e.g. `-i foo -i bar`)
    - Optional and required option values `:` (e.g. `-o[BAR] -oBAR`)
    - Key/value pairs (e.g. `-DMACRO=VALUE1)
    - Option aliases (e.g. `-v`, `-verbose`)
    - `--` to stop option parsing
    - `--help` and `--version` built-in options
    - Parsing of values to specific target types (e.g. `int`, `bool`, `enum`, etc.)) 
    - Grouping of command/options that can be activated together when a specific condition is met.

## 🧪 Example

```csharp
using System;
using XenoAtom.CommandLine;

const string _ = "";
string? name = null;
int age = 0;
List<string> messages = new List<string>();

var commandApp = new CommandApp("myexe")
{
    _,
    {"n|name=", "Your {NAME}", v => name = v},
    {"a|age=", "Your {AGE}", (int v) => age = v},
    new HelpOption(),
    _,
    "Available commands:",
    new Command("commit")
    {
        _,
        {"m|message=", "Add a {MESSAGE} to this commit", messages},
        new HelpOption(),

        // Action for the commit command
        (arguments) =>
        {
            Console.Out.WriteLine($"Committing with name={name}, age={age}");
            foreach (var message in messages)
            {
                Console.Out.WriteLine($"Commit message: {message}");
            }
            return ValueTask.FromResult(0);
        }
    },
    // Default action if no command is specified
    (_) =>
    {
        Console.Out.WriteLine($"Hello {name}! You are {age} years old.");
        return ValueTask.FromResult(0);
    }
};

await commandApp.RunAsync(args);
```

Running `myexe --help` will output:

```
Usage: myexe [Options] COMMAND

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

Running `myexe commit --help` will output:

```
Usage: myexe commit [Options]

  -m, --message=MESSAGE      Add a MESSAGE to this commit
  -h, -?, --help             Show this message and exit
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

You need to install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). Then from the root folder:

```console
$ dotnet build src -c Release
```

## 🪪 License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause).

The license also integrate the original MIT license from [Mono.Options](https://github.com/mono/mono/blob/main/mcs/class/Mono.Options/Mono.Options/Options.cs).

## 🤗 Author

Alexandre Mutel aka [xoofx](https://xoofx.github.io).
