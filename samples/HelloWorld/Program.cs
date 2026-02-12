// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;
using TerminalHost = XenoAtom.Terminal.Terminal;

// Demonstrate a command line application with a sub-command
const string _ = "";
bool showMarkup = false;
bool showVisual = false;
string? name = null;
int age = 0;
var keyValues = new List<(string Key, string? Value)>();
var commitMessages = new List<string>();
var commitFiles = new List<string>();

var enableTerminalOutput = args.Any(static arg =>
    string.Equals(arg, "--markup", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "--visual", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "/markup", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "/visual", StringComparison.OrdinalIgnoreCase));

using var session = enableTerminalOutput ? TerminalHost.Open() : null;

var commandApp = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = _ => showVisual
        ? new TerminalVisualCommandOutput()
        : showMarkup
            ? new TerminalMarkupCommandOutput()
            : DefaultCommandOutput.Instance
})
{
    new CommandUsage(),
    _,
    "Options:",
    { "markup", "Render help/errors with terminal markup", v => showMarkup = v is not null },
    { "visual", "Render help/errors with terminal visual output", v => showVisual = v is not null },
    { "D:", "Defines a {0:name} and optional {1:value}", (key, value) =>
        {
            if (key is null) throw new OptionException("The key is mandatory for a define", "D");
            keyValues.Add((key, value));
        }},
    {"n|name=", "Your {NAME}", v => name = v},
    {"a|age=", "Your {AGE}", (int v) => age = v},
    new HelpOption(),
    new CompletionCommands(), // Add completion commands
    _,
    "Available commands:",
    new Command("commit")
    {
        _,
        {"m|message=", "Add a {MESSAGE} to this commit", commitMessages},
        _,
        "Arguments:",
        { "<files>*", "Files to commit", commitFiles },
        new HelpOption(),
        (ctx, _) =>
        {
            if (name is null) throw new OptionException("Missing name argument", nameof(name));
            if (age == 0) throw new OptionException("Missing age argument", nameof(age));

            ctx.Out.WriteLine($"Committing with name={name}, age={age}");
            foreach (var message in commitMessages)
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
        if (name is null) throw new OptionException("Missing name argument", nameof(name));
        if (age == 0) throw new OptionException("Missing age argument", nameof(age));

        ctx.Out.WriteLine($"Hello {name}! You are {age} years old.");
        foreach (var keyValue in keyValues)
        {
            ctx.Out.WriteLine($"Define: {keyValue.Key} => {keyValue.Value}");
        }

        return ValueTask.FromResult(0);
    }
};

await commandApp.RunAsync(args);
