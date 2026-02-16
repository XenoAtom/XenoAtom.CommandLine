// This file is consumed by MarkdownSnippets.
// Snippets are intentionally kept verbatim to mirror markdown examples.
#pragma warning disable

namespace XenoAtom.CommandLine.Terminal.Tests.Documentation;

public static class MarkdownExamplesSnippets
{
}

#if false

// Source: readme.md
// begin-snippet: readme_md_001
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
        if (key is null) throw new CommandOptionException("The key is mandatory for a define", "D");
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
        "Options:",
        {"m|message=", "Add a {MESSAGE} to this commit", messages},
        new HelpOption(),
        _,
        "Arguments:",
        { "<files>*", "Files to commit", commitFiles },

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
// end-snippet

// Source: readme.md
// begin-snippet: readme_md_002
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Styling;

var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = _ => new TerminalVisualCommandOutput()
})
{
    new CommandUsage(),
    new TextFiglet("XenoAtom")
        .Font(FigletPredefinedFont.Standard)
        .LetterSpacing(1)
        .TextAlignment(TextAlignment.Left)
        .Style(TextFigletStyle.Default with
        {
            ForegroundBrush = Brush.LinearGradient(
                new GradientPoint(0f, 0f),
                new GradientPoint(1f, 0f),
                [
                    new GradientStop(0f, Colors.DodgerBlue),
                    new GradientStop(0.5f, Colors.White),
                    new GradientStop(1f, Colors.Orange),
                ],
                mixSpaceOverride: ColorMixSpace.Oklab),
        }),
    "Options:",
    { "n|name=", "Your {NAME}", _ => { } },
    new HelpOption(),
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_001
string? name = null;
int port = 0;

var app = new CommandApp("myexe")
{
    { "n|name=", "Your {NAME}", v => name = v },
    { "p|port=", "Server {PORT}", (int v) => port = v },
    new HelpOption(),
    (ctx, _) => ValueTask.FromResult(0)
};

var result = app.Parse(["--name", "Alice", "--port", "8080"]);

// result.HasErrors          → false
// result.ResolvedCommandPath → "myexe"
// result.OptionValues["name"][0] → "Alice"
// result.OptionValues["port"][0] → "8080"
// name → "Alice" (option callbacks are invoked)
// port → 8080
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_002
var result = app.Parse(["commit", "--message", "Hello"]);

// result.ResolvedCommandPath → "myexe commit"
// result.OptionValues["message"][0] → "Hello"
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_003
var app = new CommandApp("myexe")
{
    new CompletionCommands(),
    { "n|name=", "Your {NAME}", v => {} },
    new HelpOption(),
    new Command("build", "Build the project") { (ctx, _) => ValueTask.FromResult(0) },
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_004
var candidates = app.GetCompletions("myexe --na");
// → ["--name"]

var commandCandidates = app.GetCompletionsForTokens(["myexe", "buil"], tokenIndex: 1);
// → ["build"]
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_005
app.Options["name"].ValueCompleter = static (index, prefix) =>
    ["Alice", "Bob", "Charlie"];

app.Arguments[0].ValueCompleter = static (index, prefix) =>
    ["README.md", "src/", "tests/"];
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_006
var app = new CommandApp("myexe")
{
    new HelpOption(),
    new ResponseFileSource(),
    { "<>", "Extra arguments" },
    (ctx, arguments) =>
    {
        foreach (var arg in arguments)
            ctx.Out.WriteLine(arg);
        return ValueTask.FromResult(0);
    }
};
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_007
public class EnvironmentSource : ArgumentSource
{
    public override string Description => "Read arguments from environment";
    public override string[] GetNames() => ["@env"];
    public override bool TryGetArguments(string value, out IEnumerable<string>? arguments)
    {
        if (value.StartsWith("@env:"))
        {
            var envValue = Environment.GetEnvironmentVariable(value[5..]);
            arguments = envValue?.Split(' ') ?? [];
            return true;
        }
        arguments = null;
        return false;
    }
}
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_008
var config = new CommandConfig
{
    StrictOptionParsing = true,                    // default
    Localizer = s => s,                            // identity by default
    EnvironmentVariableResolver = Environment.GetEnvironmentVariable,  // default
    OutputFactory = runConfig => new MyOutputRenderer(),
};

var app = new CommandApp("myexe", config: config);
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_009
var runConfig = new CommandRunConfig(Width: 120, OptionWidth: 32)
{
    Out = Console.Out,
    Error = Console.Error,
    ShowLicenseOnRun = true,
};

await app.RunAsync(args, runConfig);
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_010
var app = new CommandApp("myexe", config: new CommandConfig
{
    Localizer = text => MyLocalizationService.Translate(text),
});
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_011
var envVars = new Dictionary<string, string> { ["MY_PORT"] = "8080" };

var app = new CommandApp("myexe", config: new CommandConfig
{
    EnvironmentVariableResolver = name => envVars.GetValueOrDefault(name),
});
// end-snippet

// Source: site\docs\advanced.md
// begin-snippet: site_docs_advanced_md_012
var colors = new List<Color>();

var app = new CommandApp("myexe")
{
    { "c|color=", "Console {COLOR} (" + EnumWrapper<Color>.Names + ")",
        (EnumWrapper<Color> v) => colors.Add(v) },
    (ctx, _) => ValueTask.FromResult(0)
};

enum Color { Red, Green, Blue }
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_001
string? input = null;
var app = new CommandApp("myexe")
{
    new HelpOption(),
    "Arguments:",
    { "<input>", "Input file", v => input = v },
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Input: {input}");
        return ValueTask.FromResult(0);
    }
};
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_002
{ "<input>", "Input file", v => input = v }
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_003
{ "<output>?", "Output file (optional)", v => output = v }
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_004
var files = new List<string>();
{ "<files>*", "Input files", files }
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_005
{ "<files>+", "Input files (at least one)", files }
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_006
{ "<>", "Extra arguments passed to the action" }
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_007
app.AddRemainder("Extra arguments passed to the action");
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_008
var app = new CommandApp("myexe")
{
    { "<>", "Extra arguments" },
    (ctx, arguments) =>
    {
        foreach (var arg in arguments)
            ctx.Out.WriteLine($"Arg: {arg}");
        return ValueTask.FromResult(0);
    }
};
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_009
string? input = null;
string? output = null;
var extraFiles = new List<string>();

var app = new CommandApp("myexe")
{
    new CommandUsage(),
    new HelpOption(),
    "Arguments:",
    { "<input>", "Input file", v => input = v },
    { "<output>?", "Output file (optional)", v => output = v },
    { "<extra>*", "Additional files", extraFiles },
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Input: {input}");
        ctx.Out.WriteLine($"Output: {output ?? "(none)"}");
        foreach (var f in extraFiles)
            ctx.Out.WriteLine($"Extra: {f}");
        return ValueTask.FromResult(0);
    }
};
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_010
int count = 0;
{ "<count>", "Number of items", (int v) => count = v }
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_011
{ "<input>", "Input {FILE}", v => input = v, Validate.FileExists(), false }
// end-snippet

// Source: site\docs\arguments.md
// begin-snippet: site_docs_arguments_md_012
var app = new CommandApp("myexe")
{
    { "v|verbose", "Verbose", v => {} },
    new HelpOption(),
    { "<input>", "Input file", v => input = v },
    { "<output>?", "Output file", v => output = v },
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_001
var app = new CommandApp("myexe")
{
    { "v|verbose", "Enable verbose output", v => {} },
    new HelpOption(),
    (ctx, _) =>
    {
        ctx.Out.WriteLine("Root command executed");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_002
using XenoAtom.CommandLine;

const string _ = "";
string? name = null;
var messages = new List<string>();
var files = new List<string>();

var app = new CommandApp("myexe")
{
    new CommandUsage(),
    _,
    { "n|name=", "Your {NAME}", v => name = v },
    new HelpOption(),
    _,
    "Available commands:",
    new Command("commit", "Commit changes")
    {
        _,
        "Options:",
        { "m|message=", "Commit {MESSAGE}", messages },
        new HelpOption(),
        _,
        "Arguments:",
        { "<files>*", "Files to commit", files },
        (ctx, _) =>
        {
            ctx.Out.WriteLine($"Committing as {name}");
            foreach (var msg in messages)
                ctx.Out.WriteLine($"  Message: {msg}");
            foreach (var file in files)
                ctx.Out.WriteLine($"  File: {file}");
            return ValueTask.FromResult(0);
        }
    },
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Hello, {name}! Use 'myexe commit' to commit.");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_003
var app = new CommandApp("myexe")
{
    new Command("remote")
    {
        new Command("add", "Add a remote")
        {
            { "n|name=", "Remote {NAME}", v => {} },
            (ctx, _) => ValueTask.FromResult(0)
        },
        new Command("remove", "Remove a remote")
        {
            { "n|name=", "Remote {NAME}", v => {} },
            (ctx, _) => ValueTask.FromResult(0)
        },
    },
};
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_004
var cmd = new Command("internal-debug", "Debug command") { Hidden = true };
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_005
(ctx, _) =>
{
    ctx.Out.WriteLine("Hello!");
    return ValueTask.FromResult(0);
}
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_006
(arguments) =>
{
    foreach (var arg in arguments)
        Console.WriteLine(arg);
    return ValueTask.FromResult(0);
}
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_007
async (ctx, _) =>
{
    await SomeAsyncWork();
    return 0;
}
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_008
bool advanced = false;

var app = new CommandApp("myexe")
{
    "Options:",
    { "advanced", "Activate advanced options", v => advanced = v is not null },
    new HelpOption(),
    new CommandGroup(() => advanced)
    {
        "Advanced Options:",
        { "special1", "Special option 1", v => {} },
        { "special2", "Special option 2", v => {} },
    },
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_009
new CommandGroup(() => advanced)
{
    "Advanced Commands:",
    new Command("debug", "Debug the application")
    {
        new HelpOption(),
        (ctx, _) => ValueTask.FromResult(0)
    },
}
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_010
var app = new CommandApp("myexe")
{
    "Options:",
    { "v|verbose", "Verbose", v => {} },
    new HelpOption(),
    "",
    "Available commands:",
    new Command("build", "Build the project")
    {
        (ctx, _) => ValueTask.FromResult(0)
    },
};
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_011
new HelpOption()
// Equivalent to:
// { "h|?|help", "Show this message and exit", v => { /* triggers help */ } }
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_012
new HelpOption("h|help", "Display help information")
// end-snippet

// Source: site\docs\commands.md
// begin-snippet: site_docs_commands_md_013
new VersionOption("1.2.3")
// Equivalent to:
// { "v|version", "Show the version of this command", v => { /* prints version */ } }
// end-snippet

// Source: site\docs\getting-started.md
// begin-snippet: site_docs_getting_started_md_001
using XenoAtom.CommandLine;

string? name = null;

var app = new CommandApp("greet")
{
    { "n|name=", "Your {NAME}", v => name = v },
    new HelpOption(),
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Hello, {name ?? "World"}!");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
// end-snippet

// Source: site\docs\getting-started.md
// begin-snippet: site_docs_getting_started_md_002
using XenoAtom.CommandLine;

string? name = null;
int age = 0;
bool verbose = false;

var app = new CommandApp("greet")
{
    new CommandUsage(),
    { "n|name=", "Your {NAME}", v => name = v },
    { "a|age=", "Your {AGE}", (int v) => age = v },
    { "v|verbose", "Enable verbose output", v => verbose = v is not null },
    new HelpOption(),
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Hello, {name}! You are {age} years old.");
        if (verbose)
            ctx.Out.WriteLine("(verbose mode enabled)");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
// end-snippet

// Source: site\docs\getting-started.md
// begin-snippet: site_docs_getting_started_md_003
using XenoAtom.CommandLine;

const string _ = "";
string? name = null;
var messages = new List<string>();

var app = new CommandApp("myapp")
{
    new CommandUsage(),
    _,
    { "n|name=", "Your {NAME}", v => name = v },
    new HelpOption(),
    _,
    "Available commands:",
    new Command("greet", "Greet someone")
    {
        _,
        "Options:",
        { "m|message=", "Greeting {MESSAGE}", messages },
        new HelpOption(),
        (ctx, _) =>
        {
            ctx.Out.WriteLine($"Hello, {name}!");
            foreach (var msg in messages)
                ctx.Out.WriteLine($"  {msg}");
            return ValueTask.FromResult(0);
        }
    },
    (ctx, _) =>
    {
        ctx.Out.WriteLine("Use 'myapp greet --help' for more info.");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
// end-snippet

// Source: site\docs\getting-started.md
// begin-snippet: site_docs_getting_started_md_004
using XenoAtom.CommandLine;

string? input = null;
var extraFiles = new List<string>();

var app = new CommandApp("myapp")
{
    new CommandUsage(),
    new HelpOption(),
    "Arguments:",
    { "<input>", "The input file", v => input = v },
    { "<extra>*", "Additional files", extraFiles },
    (ctx, _) =>
    {
        ctx.Out.WriteLine($"Input: {input}");
        foreach (var f in extraFiles)
            ctx.Out.WriteLine($"Extra: {f}");
        return ValueTask.FromResult(0);
    }
};

await app.RunAsync(args);
// end-snippet

// Source: site\docs\getting-started.md
// begin-snippet: site_docs_getting_started_md_005
var result = app.Parse(["--name", "Alice", "--age", "30"]);

// result.HasErrors             → false
// result.OptionValues["name"]  → ["Alice"]
// result.OptionValues["age"]   → ["30"]
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_001
var app = new CommandApp("myexe")
{
    "Options:",
    { "v|verbose", "Enable verbose output", v => {} },
    { "n|name=", "Your {NAME}", v => {} },
    new HelpOption(),
    "",
    "Available commands:",
    new Command("build", "Build the project") { (ctx, _) => ValueTask.FromResult(0) },
    new Command("test", "Run tests") { (ctx, _) => ValueTask.FromResult(0) },
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_002
new CommandUsage()
// Produces: "Usage: myexe [options] <command>"
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_003
new CommandUsage("Usage: {NAME} [--advanced] [Advanced Options]")
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_004
var app = new CommandApp("myexe")
{
    new CommandUsage(),
    new CommandUsage("Usage: {NAME} @responsefile"),
    // ...
};
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_005
const string _ = "";
var app = new CommandApp("myexe")
{
    new CommandUsage(),
    _,
    "Options:",
    { "v|verbose", "Verbose", v => {} },
    new HelpOption(),
    _,
    "Arguments:",
    { "<files>*", "Input files", new List<string>() },
    _,
    "Available commands:",
    new Command("build") { (ctx, _) => ValueTask.FromResult(0) },
    _,
    "Run 'myexe <command> --help' for more information.",
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_006
app.AddSection("Options");  // Adds "Options:"
app.AddText("Additional help text");
app.AddRemainder("Extra arguments");
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_007
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Styling;

var app = new CommandApp("myexe")
{
    new CommandUsage(),
    new TextFiglet("XenoAtom")
        .Font(FigletPredefinedFont.Standard)
        .LetterSpacing(1)
        .TextAlignment(TextAlignment.Left)
        .Style(TextFigletStyle.Default with
        {
            ForegroundBrush = Brush.LinearGradient(
                new GradientPoint(0f, 0f),
                new GradientPoint(1f, 0f),
                [
                    new GradientStop(0f, Colors.DodgerBlue),
                    new GradientStop(0.5f, Colors.White),
                    new GradientStop(1f, Colors.Orange),
                ],
                mixSpaceOverride: ColorMixSpace.Oklab)
        }),
    "Options:",
    { "n|name=", "Your {NAME}", _ => { } },
    new HelpOption(),
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_008
new Command("myexe")
{
    { new TextFiglet("XenoAtom"), "XenoAtom" },
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_009
var app = new CommandApp("myexe")
{
    LicenseHeader = () => "MyApp v1.0 - Copyright (c) 2025 MyCompany",
    // ...
};

await app.RunAsync(args, new CommandRunConfig { ShowLicenseOnRun = true });
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_010
public interface ICommandOutput
{
    void WriteHelp(Command command, CommandRunConfig runConfig);
    void WriteError(Command command, CommandRunConfig runConfig, CommandException exception);
    void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenReport report);
    void WriteVersion(Command command, CommandRunConfig runConfig, string version);
    void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText);
}
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_011
public sealed class JsonOutputRenderer : ICommandOutput
{
    public void WriteHelp(Command command, CommandRunConfig runConfig)
    {
        // Access command.Options, command.Arguments, command.SubCommands
        runConfig.Out.WriteLine("{ \"help\": \"" + command.GetFullCommandPath() + "\" }");
    }

    public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
    {
        runConfig.Error.WriteLine("{ \"error\": \"" + exception.Message + "\" }");
    }

    public void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenReport report)
    {
        foreach (var token in report.UnknownTokens)
            runConfig.Error.WriteLine("{ \"unknown\": \"" + token.Token + "\" }");
    }

    public void WriteVersion(Command command, CommandRunConfig runConfig, string version)
        => runConfig.Out.WriteLine("{ \"version\": \"" + version + "\" }");

    public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText)
        => runConfig.Out.WriteLine(licenseText);
}
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_012
var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = runConfig => new JsonOutputRenderer()
});
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_013
app.ShowHelp(new JsonOutputRenderer());
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_014
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;

var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = _ => new TerminalMarkupCommandOutput()
});
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_015
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;

var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = _ => new TerminalVisualCommandOutput(new TerminalVisualOutputOptions
    {
        UseTableForOptions = true,
        UseTableForArguments = true,
        UseTableForCommands = true,
        SectionGroupMinWidth = 70,
        ErrorGroupMinWidth = 70,
    })
});
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_016
var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = _ => new TerminalVisualCommandOutput()
})
{
    new CommandUsage(),
    "Options:",                          // ← section header
    { "n|name=", "Your {NAME}", _ => {} },
    new HelpOption(),
    "Arguments:",                        // ← section header
    { "<files>*", "Input files", new List<string>() },
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\help-output.md
// begin-snippet: site_docs_help_output_md_017
var helpVisual = app.ToHelpVisual(new TerminalVisualOutputOptions
{
    OptionPrototypeStyle = "[accent]",
    SectionGroupMinWidth = 70,
});

XenoAtom.Terminal.Terminal.Write(helpVisual);
// end-snippet

// Source: site\docs\migration-2.0.md
// begin-snippet: site_docs_migration_2_0_md_001
var hidden = new Command("secret");
hidden.Hidden = true;

app.Options["name"].EnvironmentVariable = "APP_NAME";
// end-snippet

// Source: site\docs\migration-2.0.md
// begin-snippet: site_docs_migration_2_0_md_002
var hidden = new Command("secret")
{
    Hidden = true
};

app.Add("n|name=", "Name", value => { }, "APP_NAME");
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_001
var app = new CommandApp("myexe")
{
    { "o|output=", "The target output {FILE}", v => target = v },
};

// Equivalent:
app.Add("o|output=", "The target output {FILE}", v => target = v);
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_002
bool verbose = false;
var app = new CommandApp("myexe")
{
    { "v|verbose", "Enable verbose output", v => verbose = v is not null },
};
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_003
await app.RunAsync(["-v"]);   // verbose == true
await app.RunAsync(["-v+"]);  // verbose == true
await app.RunAsync(["-v-"]);  // verbose == false
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_004
string? name = null;
var app = new CommandApp("myexe")
{
    { "n|name=", "Your {NAME}", v => name = v },
};
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_005
string? output = null;
var app = new CommandApp("myexe")
{
    { "o:", "Output file (optional)", v => output = v },
};
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_006
var app = new CommandApp("myexe")
{
    { "D:", "Define {0:NAME} and optional {1:VALUE}", (key, value) =>
    {
        if (key is null) throw new CommandOptionException("Missing macro name", "D");
        Console.WriteLine($"Macro: {key} = {value}");
    }},
    { "I|macro=", "Define {0:NAME} and required {1:VALUE}", (key, value) =>
    {
        Console.WriteLine($"Macro: {key} = {value}");
    }},
};
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_007
{ "P={->}", "Define {0:NAME} and {1:VALUE}", (k, v) => Console.WriteLine($"{k} -> {v}") },
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_008
int port = 0;
var app = new CommandApp("myexe")
{
    { "p|port=", "Server {PORT}", (int v) => port = v },
};
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_009
var names = new List<string>();
var ports = new List<int>();

var app = new CommandApp("myexe")
{
    { "n|name=", "A {NAME}", names },
    { "p|port=", "A {PORT}", ports },
};
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_010
var colors = new List<Color>();

var app = new CommandApp("myexe")
{
    { "c|color=", "The {COLOR} (" + EnumWrapper<Color>.Names + ")", (EnumWrapper<Color> v) => colors.Add(v) },
};

enum Color { Red, Green, Blue }
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_011
bool a = false, b = false, c = false;
var app = new CommandApp("myexe")
{
    { "a", "Flag A", v => a = v is not null },
    { "b", "Flag B", v => b = v is not null },
    { "c", "Flag C", v => c = v is not null },
};

await app.RunAsync(["-abc"]); // a == true, b == true, c == true
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_012
string? file = null;
var app = new CommandApp("myexe")
{
    { "x", "Extract", v => {} },
    { "f=", "Input {FILE}", v => file = v },
};

await app.RunAsync(["-xfarchive.tar"]); // file == "archive.tar"
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_013
{ "n|name=", "Your {NAME}", v => name = v }
// Help: -n, --name=NAME            Your NAME
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_014
{ "D:", "Define {0:KEY} and optional {1:VALUE}", (k, v) => {} }
// Help: -D[=KEY:VALUE]             Define KEY and optional VALUE
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_015
{ "secret=", "Secret option", v => {}, true }
// end-snippet

// Source: site\docs\options.md
// begin-snippet: site_docs_options_md_016
int port = 0;
var includes = new List<string>();

var app = new CommandApp("myexe")
{
    { "p|port=", "Server {PORT}", (int v) => port = v, "MY_PORT" },
    { "i|include=", "Include {PATH}", includes, "MY_INCLUDES", Path.PathSeparator },
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\readme.md
// begin-snippet: site_docs_readme_md_001
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
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_001
int port = 0;
string? email = null;
string? input = null;

var app = new CommandApp("myexe")
{
    { "p|port=", "Server {PORT}", (int v) => port = v, Validate.Range(1, 65535) },
    { "e|email=", "Contact {EMAIL}", v => email = v,
        Validate.That<string>(v => v.Contains('@'), "The value must be a valid email address."), false },
    { "<input>", "Input {FILE}", v => input = v, Validate.FileExists(), false },
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_002
{ "p|port=", "Port", (int v) => port = v, Validate.Range(1, 65535) }
{ "t|threads=", "Threads", (int v) => threads = v, Validate.Positive<int>() }
{ "r|retries=", "Retries", (int v) => retries = v, Validate.NonNegative<int>() }
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_003
{ "n|name=", "Name", v => name = v, Validate.NonEmpty(), false }
{ "e|email=", "Email", v => email = v, Validate.Matches(@"^[^@]+@[^@]+$", "Must be a valid email."), false }
{ "l|level=", "Level", v => level = v, Validate.OneOf("debug", "info", "warn", "error"), false }
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_004
{ "<input>", "Input file", v => input = v, Validate.FileExists(), false }
{ "o|output-dir=", "Output dir", v => dir = v, Validate.DirectoryExists(), false }
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_005
{ "p|port=", "Port", (int v) => port = v,
    Validate.That<int>(v => v % 2 == 0, "Port must be an even number.") }
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_006
{ "p|port=", "Port", (int v) => port = v,
    Validate.Chain(
        Validate.Range(1, 65535),
        Validate.That<int>(v => v != 80, "Port 80 is reserved.")
    )
}
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_007
var app = new CommandApp("myexe")
{
    { "j|json", "Output JSON", _ => {} },
    { "x|xml", "Output XML", _ => {} },
    { "c|csv", "Output CSV", _ => {} },
    new MutuallyExclusiveConstraint("json", "xml", "csv"),
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_008
app.AddMutuallyExclusive("json", "xml", "csv");
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_009
var app = new CommandApp("myexe")
{
    { "u|user=", "User", _ => {} },
    { "p|password=", "Password", _ => {} },
    new RequiresConstraint("password", "user"),
    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_010
app.AddRequires("password", "user");
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_011
var app = new CommandApp("myexe")
{
    { "j|json", "Output JSON", _ => {} },
    { "x|xml", "Output XML", _ => {} },
    { "v|verbose", "Verbose output", _ => {} },
    { "q|quiet", "Quiet output", _ => {} },
    { "u|user=", "User", _ => {} },
    { "p|password=", "Password", _ => {} },
    { "tls-cert=", "TLS cert path", _ => {} },
    { "tls-key=", "TLS key path", _ => {} },

    new MutuallyExclusiveConstraint("json", "xml"),
    new MutuallyExclusiveConstraint("verbose", "quiet"),
    new RequiresConstraint("password", "user"),
    new RequiresConstraint("tls-cert", "tls-key"),

    (ctx, _) => ValueTask.FromResult(0)
};
// end-snippet

// Source: site\docs\validation.md
// begin-snippet: site_docs_validation_md_012
app.AddMutuallyExclusive("json", "xml");
app.AddMutuallyExclusive("verbose", "quiet");
app.AddRequires("password", "user");
app.AddRequires("tls-cert", "tls-key");
// end-snippet

#endif


