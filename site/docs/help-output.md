---
title: "Help & Output"
---

# Help & Output

XenoAtom.CommandLine auto-generates help text from your declarations and provides a fully pluggable output system.

## Help Text Basics

Every string added to a command appears as descriptive text in the help output. Options, arguments, and commands are rendered in declaration order:

```csharp
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
```

Running `myexe --help`:

```
Usage: myexe [options] <command>

Options:
  -v, --verbose              Enable verbose output
  -n, --name=NAME            Your NAME
  -h, -?, --help             Show this message and exit

Available commands:
  build                      Build the project
  test                       Run tests
```

## CommandUsage

`CommandUsage` controls the `Usage:` line at the top of help output.

### Default Usage

```csharp
new CommandUsage()
// Produces: "Usage: myexe [options] <command>"
```

The default format is `"Usage: {NAME} {SYNTAX}"` where:
- `{NAME}` is replaced with the full command path (e.g. `myexe commit`).
- `{SYNTAX}` is auto-generated from declared options, arguments, and sub-commands.

### Custom Usage

```csharp
new CommandUsage("Usage: {NAME} [--advanced] [Advanced Options]")
```

### Multiple Usage Lines

You can add multiple `CommandUsage` entries to show alternative invocations:

```csharp
var app = new CommandApp("myexe")
{
    new CommandUsage(),
    new CommandUsage("Usage: {NAME} @responsefile"),
    // ...
};
```

### No Usage Line

If no `CommandUsage` is declared, a default one is automatically shown as the first line of help. To display exactly what you declare, always add at least one `CommandUsage`.

## Formatting with Text

Use empty strings for blank lines and plain strings for section headers:

```csharp
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
```

You can also use explicit helper methods when building commands without collection initializers:

```csharp
app.AddSection("Options");  // Adds "Options:"
app.AddText("Additional help text");
app.AddRemainder("Extra arguments");
```

## Inline Visual Nodes

When using `XenoAtom.CommandLine.Terminal`, you can add [XenoAtom.Terminal.UI](https://xenoatom.github.io/terminal) `Visual` controls directly in command initializers.

```csharp
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
```

You can also provide fallback text for non-visual renderers:

```csharp
new Command("myexe")
{
    { new TextFiglet("XenoAtom"), "XenoAtom" },
    (ctx, _) => ValueTask.FromResult(0)
};
```

Rendering behavior:
- `DefaultCommandOutput`: renders visual nodes as preformatted text blocks.
- `TerminalMarkupCommandOutput`: writes visual nodes inline via `Terminal.Write(visual)`.
- `TerminalVisualCommandOutput` / `ToHelpVisual()`: inserts visual nodes directly in the visual tree.

## License Header

Display a license banner before command execution:

```csharp
var app = new CommandApp("myexe")
{
    LicenseHeader = () => "MyApp v1.0 - Copyright (c) 2025 MyCompany",
    // ...
};

await app.RunAsync(args, new CommandRunConfig { ShowLicenseOnRun = true });
```

The license header is printed once before the command action runs. Set `ShowLicenseOnRun = false` to suppress it.

## Custom Output Rendering

All library-generated output — help, errors, unknown-token diagnostics, version, and license — can be completely replaced by implementing `ICommandOutput` and setting `CommandConfig.OutputFactory`.

### ICommandOutput Interface

```csharp
public interface ICommandOutput
{
    void WriteHelp(Command command, CommandRunConfig runConfig);
    void WriteError(Command command, CommandRunConfig runConfig, CommandException exception);
    void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenReport report);
    void WriteVersion(Command command, CommandRunConfig runConfig, string version);
    void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText);
}
```

### Example: Custom Output Renderer

```csharp
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
```

### Registering the Custom Renderer

```csharp
var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = runConfig => new JsonOutputRenderer()
});
```

### One-Off Help Rendering

You can render help with a specific renderer without changing the configuration:

```csharp
app.ShowHelp(new JsonOutputRenderer());
```

### CommandOutputHelper

The `CommandOutputHelper` class provides utility methods for building custom renderers:

- `GetFullCommandPath(command)` — full path like `"myexe commit"`
- `GetDefaultUsageSyntax(command)` — auto-generated syntax string
- `GetVisibleOptions(command)` — options not hidden
- `GetVisibleArguments(command)` — arguments not hidden
- `GetVisibleSubCommands(command)` — sub-commands not hidden
- `GetOptionValueName(option, valueIndex)` — value placeholder name
- `GetDescriptionText(description)` — processed description text

## Descriptor Contracts

For custom outputs and custom command nodes, the following contracts apply:

- `ICommandNodeDescriptor.Description` should be plain help text intent (not renderer-specific markup).
- `IHelpPreformattedContent.WriteTo(...)` is the preformatted contract for verbatim text output.
- If a node provides both interfaces, text outputs should prefer `IHelpPreformattedContent` and use `Description` as fallback when preformatted rendering is not supported.
- Public extensibility is centered on `ICommandOutput` and these descriptor interfaces; broader custom node internals are intentionally kept as an implementation boundary for official package integrations.

## Terminal Package

For rich colored and visual help output, install the optional `XenoAtom.CommandLine.Terminal` package:

```sh
dotnet add package XenoAtom.CommandLine.Terminal
```

> **Note:** This package targets `net10.0` and depends on `XenoAtom.Terminal.UI` (which pulls `XenoAtom.Terminal` transitively).

### TerminalMarkupCommandOutput

Renders help, errors, and version output with terminal markup (colors/bold):

```csharp
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;

var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = _ => new TerminalMarkupCommandOutput()
});
```

### TerminalVisualCommandOutput

Renders help and errors as [XenoAtom.Terminal.UI](https://xenoatom.github.io/terminal) visuals with borders, tables, and structured layout:

```csharp
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
```

For one-shot rendering, `Terminal.Write(...)` is lazily initialized and does not require an explicit terminal session.

### Section Grouping with `:`

When using `TerminalVisualCommandOutput` (or `ToHelpVisual()`), a help text line ending with `:` is treated as a **section header**. The following rows are rendered in a grouped visual block (e.g. with a rounded border):

```csharp
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
```

`Options:` and `Arguments:` become distinct visual groups in the rendered output.

### Standalone Visual Help

Generate a help visual for embedding in fullscreen [XenoAtom.Terminal.UI](https://xenoatom.github.io/terminal) applications:

```csharp
var helpVisual = app.ToHelpVisual(new TerminalVisualOutputOptions
{
    OptionPrototypeStyle = "[accent]",
    SectionGroupMinWidth = 70,
});

XenoAtom.Terminal.Terminal.Write(helpVisual);
```

### Choosing a Renderer

{.table}
| Renderer | Output | Best for |
|---|---|---|
| `DefaultCommandOutput` | Plain text | Simple CLIs, piped output |
| `TerminalMarkupCommandOutput` | Colored markup | Interactive terminals |
| `TerminalVisualCommandOutput` | Visual tables/borders | Rich interactive applications |

## Error Output

XenoAtom.CommandLine provides informative error messages by default:

### Unknown Options

```
myexe: Unknown option: --unkown
Did you mean: --unknown
Use `myexe --help` for usage.
```

### Inactive Options

When an option exists but is inside an inactive `CommandGroup`:

```
myexe: Unknown option: --special1
Note: `--special1` matches an option that is currently inactive in this context.
Use `myexe --help` for usage.
```

### Strict Option Parsing

By default, `CommandConfig.StrictOptionParsing` is `true`. Unknown tokens starting with `-` or `--` are treated as errors instead of being silently passed as positional arguments.

This does **not** apply to `/`-prefixed tokens, so POSIX paths like `/mnt/home` can be passed as positional arguments.

Use `--` to pass values starting with `-`:

```sh
myexe -- -5 --not-an-option
```

## Next Steps

- [Options](options.md) — full option reference
- [Commands](commands.md) — sub-commands and groups
- [Validation & Constraints](validation.md) — validate values
- [Advanced Topics](advanced.md) — parse API, completions, and configuration

