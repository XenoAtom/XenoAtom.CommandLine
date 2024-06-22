// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
using XenoAtom.CommandLine;

// Demonstrate a more complex command line application with sub-commands
// Run with --help to see the available commands
const string _ = "";
bool showAdvanced = false;
var enums = new List<TestEnum>();
var keyValues = new List<(string Key, string? Value)>();
var files = new List<string>();
bool extract = false;
bool create = false;
bool list = false;
bool special1 = false;
bool special2 = false;
int specialIncrease = 0;
int specialDecrease = 0;

var hello_files = new List<string>();

string? name = null;
int age = 0;

var app = new CommandApp("multi")
{
    new CommandUsage(),
    _,
    "Options:",
    // gcc-like options
    { "D:", "Add a marco {0:NAME} and optional {1:VALUE}", (k, v) =>
        {
            if (k is null) throw new OptionException("Name is required", "D");
            keyValues.Add((k, v));
        }
    },

    // tar-like options
    { "f=", "The input {FILE}", files },
    { "x", "Extract the file", v => extract = v != null },
    { "c", "Create the file", v => create = v != null },
    { "t", "List the file", v => list = v != null },
    { "a|advanced", "Show advanced options", v => showAdvanced = v != null },
    // others
    new HelpOption(),
    new VersionOption("1.2.3"),
    // A group of options only shown when --advanced is specified
    new CommandGroup(() => showAdvanced)
    {
        _,
        "Advanced Options:",
        { "special1", "This is a special option 1", v => special1 = v != null },
        { "special2", "This is a special option 2",  v => special2 = v != null },
    },
    _,
    "Available commands:",
    new Command("hello", "This is a hello command")
    {
        _,
        "Options:",
        { "n|name=", "This is a name", v => { name = v; } },
        { "a|age=", "Sets the {AGE}", (int v) => age = v > 200 ? throw new ArgumentException("Age must be <= 200") : v },
        new HelpOption(),
        {"<>", "[files]*", hello_files},

        (_) =>
        {
            Console.WriteLine($"Hello name={name}, age={age}");
            foreach (var file in hello_files)
            {
                Console.WriteLine($"Hello File {file}");
            }
            return ValueTask.FromResult(age > 100 ? 1 : 0);
        }
    },
    new Command("world", "This is a world command")
    {
        new CommandUsage("Usage: {{NAME}} [Options] [files]* @file"),
        _,
        "Options:",
        { "e|enum=", $"This is an {{ENUM}} accepting the following values: {EnumWrapper<TestEnum>.Names}", (EnumWrapper<TestEnum> v) => { enums.Add(v); } },
        new HelpOption(),
        new ResponseFileSource(),

        (ctx,arguments) =>
        {
            foreach (var file in arguments)
            {
                Console.WriteLine($"World {file}");
            }

            foreach (var enumValue in enums)
            {
                Console.WriteLine($"World Enum {enumValue}");
            }
            return ValueTask.FromResult(0);
        }
    },
    // A group of options only shown when --advanced is specified
    new CommandGroup(() => showAdvanced)
    {
        _,
        "Advanced Commands:",
        new Command("chroot", "This is an advanced command 1")
        {
            _,
            "Options:",
            { "M=", "Add a marco {0:NAME} and mandatory {1:VALUE}", (k, v) => keyValues.Add((k, v)) },
            { "P={->}", "Add a marco {0:NAME} and mandatory {1:VALUE}", (k, v) => keyValues.Add((k, v)) },
            { "this-is-a-very-long-option-that-doesnt-fit-on-the-column", "This is the long option", v => {}},
            {"i", s =>
                {
                    if (s != null)
                    {
                        specialIncrease++;
                    }
                    else
                    {
                        specialDecrease++;
                    }
                }
            },
            new HelpOption(),
            (arguments) =>
            {
                Console.WriteLine($"specialIncrease: {specialIncrease}");
                Console.WriteLine($"specialDecrease: {specialDecrease}");

                foreach (var file in arguments)
                {
                    Console.WriteLine($"chroot {file}");
                }

                // Macros
                foreach (var keyValue in keyValues)
                {
                    Console.WriteLine($"Macro: {keyValue.Key} => {keyValue.Value}");
                }

                return ValueTask.FromResult(0);
            }
        },

    },
    _,
    new CommandUsage("Run '{NAME} [command] --help' for more information on a command."),

    (arguments) =>
    {
        Console.WriteLine($"Extract: {extract}");
        Console.WriteLine($"Create: {create}");
        Console.WriteLine($"List: {list}");
        Console.WriteLine($"Special1: {special1}");
        Console.WriteLine($"Special2: {special2}");
        Console.WriteLine($"ShowAdvanced: {showAdvanced}");

        // Files
        foreach (var file in files)
        {
            Console.WriteLine($"File: {file}");
        }
        
        // Macros
        foreach (var keyValue in keyValues)
        {
            Console.WriteLine($"Macro: {keyValue.Key} => {keyValue.Value}");
        }

        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);

enum TestEnum
{
    Value1,
    Value2,
    Value3
}