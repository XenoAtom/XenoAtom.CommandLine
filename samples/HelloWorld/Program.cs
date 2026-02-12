// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Styling;

// Demonstrate a command line application with a sub-command
const string _ = "";
bool showMarkup = false;
bool showVisual = false;
string? name = null;
int age = 0;
var keyValues = new List<(string Key, string? Value)>();
var commitMessages = new List<string>();
var commitFiles = new List<string>();

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
    CreateBanner(),
    _,
    "Options:",
    { "markup", "Render help/errors with terminal markup", v => showMarkup = v is not null },
    { "visual", "Render help/errors with terminal visual output", v => showVisual = v is not null },
    { "D:", "Defines a {0:name} and optional {1:value}", (key, value) =>
        {
            if (key is null) throw new CommandOptionException("The key is mandatory for a define", "D");
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
        new CommandUsage(),
        CreateBanner(),
        _,
        "Options:",
        {"m|message=", "Add a {MESSAGE} to this commit", commitMessages},
        new HelpOption(),
        _,
        "Arguments:",
        { "<files>*", "Files to commit", commitFiles },
        (ctx, _) =>
        {
            if (name is null) throw new CommandOptionException("Missing name argument", nameof(name));
            if (age == 0) throw new CommandOptionException("Missing age argument", nameof(age));

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
        if (name is null) throw new CommandOptionException("Missing name argument", nameof(name));
        if (age == 0) throw new CommandOptionException("Missing age argument", nameof(age));

        ctx.Out.WriteLine($"Hello {name}! You are {age} years old.");
        foreach (var keyValue in keyValues)
        {
            ctx.Out.WriteLine($"Define: {keyValue.Key} => {keyValue.Value}");
        }

        return ValueTask.FromResult(0);
    }
};

await commandApp.RunAsync(args);

static TextFiglet CreateBanner()
{
    var gradientBrush = Brush.LinearGradient(
        new GradientPoint(0f, 0f),
        new GradientPoint(1f, 0f),
        [
            new GradientStop(0f, Colors.DodgerBlue),
            new GradientStop(0.5f, Colors.White),
            new GradientStop(1f, Colors.Orange),
        ],
        mixSpaceOverride: ColorMixSpace.Oklab);

    return new TextFiglet("XenoAtom")
        .Font(FigletPredefinedFont.Standard)
        .LetterSpacing(1)
        .TextAlignment(TextAlignment.Left)
        .Style(TextFigletStyle.Default with { ForegroundBrush = gradientBrush });
}
