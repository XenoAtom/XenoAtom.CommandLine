// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
using XenoAtom.CommandLine;

// Demonstrate a plain command line application without using sub-commands
const string _ = "";
bool flag = false;
string? name = null;
int age = 0;
var files = new List<string>();

var commandApp = new CommandApp()
{
    new CommandUsage(),
    _,
    "Options:",
    {"f|flag", "This is a flag", v => flag = v != null },
    {"n|name=", "Your {NAME}", v => name = v},
    {"a|age=", "Your {AGE}", (int v) => age = v},
    new HelpOption(),
    new CompletionCommands(), // Add completion commands
    _,
    "Arguments:",
    { "<files>+", "Input files", files },
    // Run the command
    (_) =>
    {
        if (name == null) throw new OptionException("Missing name argument", nameof(name));
        if (age == 0) throw new OptionException("Missing age argument", nameof(age));

        Console.Out.WriteLine($"Hello {name}! You are {age} years old with flag = {flag}");
        int index = 0;
        foreach (var file in files)
        {
            Console.Out.WriteLine($"File[{index}]: {file}");
            index++;
        }
        return ValueTask.FromResult(0);
    }
};

await commandApp.RunAsync(args);
