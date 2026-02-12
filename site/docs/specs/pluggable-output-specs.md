---
discard: true
title: "XenoAtom.CommandLine — Pluggable Output & Help Rendering"
---

# XenoAtom.CommandLine — Pluggable Output & Help Rendering

**Version scope:** v2.0 (preferred; designed to be additive)  
**Date:** February 2026  
**Status:** Draft

---

## Table of Contents

1. [Motivation](#1-motivation)
2. [Current State Analysis](#2-current-state-analysis)
3. [Design Goals](#3-design-goals)
4. [API Design](#4-api-design)
5. [Default Implementation Behavior](#5-default-implementation-behavior)
6. [Usage Examples](#6-usage-examples)
7. [Integration with TUI / Rich Rendering Libraries](#7-integration-with-tui--rich-rendering-libraries)
8. [Interaction with Other Planned Features](#8-interaction-with-other-planned-features)
9. [Implementation Notes](#9-implementation-notes)
10. [Migration & Backward Compatibility](#10-migration--backward-compatibility)

---

## 1. Motivation

Today, all output produced by XenoAtom.CommandLine — help text, error messages, version display, and usage hints — is rendered by private methods inside `Command` that write directly to `TextWriter` streams (`CommandRunConfig.Out` and `CommandRunConfig.Error`). There is no way for a library consumer or extension package to override how this content is presented.

This is limiting for several scenarios:

- **Terminal UI frameworks** (e.g., XenoAtom.Terminal.UI) that want to render help as styled tables, trees, or panels with colors and formatting.
- **Machine-readable output** — producing help in JSON, Markdown, or HTML instead of plain text.
- **Custom error rendering** — displaying errors with diagnostic annotations, colored highlights, or structured formats.
- **Embedded / hosted scenarios** — collecting output into a data model (e.g., for an IDE plugin) rather than printing to a console.

The goal is to introduce a clean extension point so that all output produced by the library flows through a user-configurable handler, while keeping the current usage model intact and avoiding breaking changes (additive API only).

---

## 2. Current State Analysis

### 2.1 Output Paths

All output originates from a small number of internal sites in `Command.cs` and `VersionOption.cs`:

{.table}
| Output Kind | Method(s) | Target Stream | Triggered By |
|---|---|---|---|
| **Help** | `Command.ShowHelp(CommandRunConfig)` (public) + private helpers: `WriteOptionPrototype`, `WriteDescription`, `ShowHelp(runConfig, command, name)`, `GetDefaultUsage`, `GetDefaultUsageSyntax` | `runConfig.Out` | `HelpOption` sets `ShouldShowHelp = true`; `RunAsync` calls `ShowHelp` |
| **Error: exception** | `WriteCommandException` (private) | `runConfig.Error` | `catch (CommandException)` in `RunAsync` |
| **Error: unknown command/option** | `WriteUnknownCommandOrOption` (private) | `runConfig.Error` | Sub-command dispatch failure in `RunAsync` |
| **Error: unknown options** | `WriteUnknownOptions` (private) | `runConfig.Error` | No action + leftover tokens in `RunAsync` |
| **Version** | `VersionOption.OnParseComplete` | `runConfig.Out` | User passes `--version` |
| **License header** | `RunAsync` / `ShowHelp` | `runConfig.Out` | `CommandApp.LicenseHeader` is set |

### 2.2 Structured Data Already Exposed

The `Command` class already exposes rich structured data that a custom renderer can use:

- `Command.Name`, `Command.Description`, `Command.GetFullCommandPath()`
- `Command.Options` — `ReadOnlyDictionary<string, Option>` with each option's `Prototype`, `Description`, `OptionValueType`, `MaxValueCount`, `Hidden`, `Names`
- `Command.SubCommands` — `ReadOnlyDictionary<string, Command>`
- `Command.Arguments` — `ReadOnlyCollection<CommandArgument>` with `Prototype`, `Description`, `Cardinality`, `Optional`, `IsList`, `Hidden`, `GetDisplayName()`
- `Command.Nodes` — ordered `ReadOnlyCollection<CommandNode>` preserving declaration order (options, groups, arguments, usage nodes, sub-commands interleaved)
- `CommandUsage` node — custom usage line with `{NAME}` and `{SYNTAX}` markers
- `ArgumentSource` subclasses (e.g., `ResponseFileSource`) — with `GetNames()`, `Description`
- `ICommandNodeDescriptor` — implemented by `Option`, `Command`, `CommandArgument`, `CommandUsage`, `ArgumentSource`
- `CommandNode.IsActive()`, `CommandNode.IsThisNodeActive` — conditional visibility
- `CommandGroup` — logical grouping with `Func<bool>` activation

### 2.3 Limitations

1. **No hook point.** Help rendering is a monolithic `ShowHelp` method. Error rendering is split across three private methods. There is no interface, delegate, or virtual method a consumer can override.
2. **Plain-text only.** All rendering writes raw strings to `TextWriter`. No structured intermediate model is produced.
3. **`VersionOption` writes directly.** The version output bypasses any centralized rendering path.
4. **Layout parameters are on `CommandRunConfig`.** `Width` and `OptionWidth` are plain-text-specific concerns baked into the run config record.

---

## 3. Design Goals

1. **Single default configuration point.** A consumer sets one property (on `CommandConfig`) to replace all output rendering for an app.
2. **Per-invocation overrides.** A consumer can render help (or run a command) using a different output implementation for a single invocation (e.g., `--help-json`) without rebuilding the command tree.
3. **All output flows through the handler.** Help, errors, version, and license header — every user-visible output is delegated to the configured handler.
4. **Structured data, not strings.** The handler receives the `Command` object (which already exposes options, arguments, sub-commands, etc.) along with contextual information — not pre-formatted strings.
5. **Default matches current behavior.** When no custom handler is configured, the library produces exactly the same output it does today.
6. **No breaking changes.** Existing public methods (`ShowHelp`, `RunAsync`, constructors) remain source- and binary-compatible. The change is additive (new types + one new config property and one new `ShowHelp` overload).
7. **AOT / trimming safe.** No reflection. The handler interface uses concrete types and sealed implementations.
8. **Extensible for future features.** The handler can be extended (via default interface methods or new overloads) as new output scenarios arise (e.g., validation errors, constraint violations from the new-features spec).

---

### 3.1 Non-Goals

- **Not a logging framework.** This does not replace application logging or tracing; it only covers library-produced user-facing output.
- **Not a UI toolkit.** The core library does not add rich rendering, colors, tables, or dependencies; those belong in extension packages.
- **Does not intercept user output.** Output written by user command actions to `ctx.Out` / `ctx.Error` remains unchanged.
- **Does not guarantee identical output for custom handlers.** Only `DefaultCommandOutput` guarantees byte-identical compatibility with previous versions.

## 4. API Design

### 4.1 `ICommandOutput` Interface

A single interface defines all output operations that the library can produce:

```csharp
/// <summary>
/// Identifies where a diagnostic value originated from.
/// </summary>
public enum CommandDiagnosticSource
{
    /// <summary>
    /// The value originated from the command line token stream passed to <c>RunAsync</c>.
    /// </summary>
    CommandLine,

    /// <summary>
    /// The value originated from a response file (e.g. <c>@args.txt</c>).
    /// </summary>
    ResponseFile,

    /// <summary>
    /// The value originated from an environment variable fallback.
    /// </summary>
    EnvironmentVariable,

    /// <summary>
    /// Other or unknown origin.
    /// </summary>
    Other,
}

/// <summary>
/// Identifies a span within a token in a command line token stream.
/// </summary>
/// <param name="TokenIndex">The 0-based token index within the invocation token list.</param>
/// <param name="Start">The 0-based character start within the token string.</param>
/// <param name="Length">The length within the token string.</param>
public readonly record struct CommandTokenSpan(int TokenIndex, int Start, int Length);

/// <summary>
/// Provides optional structured diagnostic context for rendering rich error output.
/// </summary>
/// <remarks>
/// This data is intended for presentation only (e.g., re-printing the invocation and
/// underlining the token that caused the error). It must not include secret values
/// (e.g., environment variable contents).
/// </remarks>
public readonly record struct CommandDiagnostic(
    CommandDiagnosticSource Source,
    string? SourceName,
    CommandNode? Node,
    IReadOnlyList<string>? Tokens,
    CommandTokenSpan? TokenSpan);

/// <summary>
/// Identifies how unknown tokens should be described.
/// </summary>
public enum UnknownTokenKind
{
    /// <summary>
    /// The token could be either a sub-command name or an option-like token,
    /// depending on parsing mode.
    /// </summary>
    UnknownCommandOrOption,

    /// <summary>
    /// The token is treated as an unknown option-like token.
    /// </summary>
    UnknownOption,
}

/// <summary>
/// Describes an unknown token along with suggestions and diagnostics.
/// </summary>
/// <param name="Token">The unrecognized token.</param>
/// <param name="Suggestions">Suggested corrections, if any (e.g., from fuzzy matching).</param>
/// <param name="InactiveMatchMessage">A note if the token matches an inactive command/option, or null.</param>
/// <param name="TokenSpan">The optional location of this token in the invocation token stream.</param>
public readonly record struct UnknownTokenInfo(
    string Token,
    IReadOnlyList<string> Suggestions,
    string? InactiveMatchMessage,
    CommandTokenSpan? TokenSpan);

/// <summary>
/// Defines the output handler for all user-visible output produced by the command-line
/// parser: help text, error messages, version display, and license headers.
/// </summary>
/// <remarks>
/// Implement this interface to provide custom rendering (e.g., colored terminal UI,
/// structured formats, or machine-readable output).
/// The <see cref="Command"/> passed to each method exposes all structured data
/// (options, arguments, sub-commands, nodes) needed to render output.
/// </remarks>
public interface ICommandOutput
{
    /// <summary>
    /// Renders the help/usage for the specified command.
    /// </summary>
    /// <param name="command">The command whose help should be displayed.
    /// Use <see cref="Command.Options"/>, <see cref="Command.Arguments"/>,
    /// <see cref="Command.SubCommands"/>, and <see cref="CommandContainer.Nodes"/>
    /// to access the structured content.</param>
    /// <param name="runConfig">The run configuration providing output streams and layout hints.</param>
    void WriteHelp(Command command, CommandRunConfig runConfig);

    /// <summary>
    /// Renders a command exception (parse error, validation error, etc.).
    /// </summary>
    /// <param name="command">The command that was being parsed when the error occurred.</param>
    /// <param name="runConfig">The run configuration providing output streams.</param>
    /// <param name="exception">The exception describing the error.</param>
    /// <remarks>
    /// The exception may carry a <c>CommandDiagnostic</c> payload that enables rich rendering
    /// such as underlining the offending token in the original invocation.
    /// </remarks>
    void WriteError(Command command, CommandRunConfig runConfig, CommandException exception);

    /// <summary>
    /// Renders an error report for unknown token(s).
    /// </summary>
    /// <param name="command">The command context where the unknown token was encountered.</param>
    /// <param name="runConfig">The run configuration providing output streams.</param>
    /// <param name="kind">The kind of unknown-token report to render.</param>
    /// <param name="unknownTokens">One or more unknown tokens.</param>
    void WriteUnknownTokens(
        Command command,
        CommandRunConfig runConfig,
        UnknownTokenKind kind,
        IReadOnlyList<UnknownTokenInfo> unknownTokens);

    /// <summary>
    /// Renders the version string.
    /// </summary>
    /// <param name="command">The root command (or sub-command) that owns the version option.</param>
    /// <param name="runConfig">The run configuration providing output streams.</param>
    /// <param name="version">The version string to display.</param>
    void WriteVersion(Command command, CommandRunConfig runConfig, string version);

    /// <summary>
    /// Renders the license header, if any.
    /// </summary>
    /// <param name="command">The root command app.</param>
    /// <param name="runConfig">The run configuration providing output streams.</param>
    /// <param name="licenseText">The license header text.</param>
    void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText);
}
```

#### Design Notes

- **`WriteUnknownTokens`** unifies the current unknown-token outputs (`WriteUnknownCommandOrOption`, `WriteUnknownOptions`, and the strict-parsing unknown-option case). It is called once per error report and can render a single "Use `... --help` for usage." hint.
- **`WriteError`** handles all `CommandException` types (including `OptionException`, `CommandArgumentException`, and future validation errors). The handler can inspect the exception type for special rendering.
- **`WriteVersion`** and **`WriteLicenseHeader`** are separated rather than being lumped into a generic "write line" method. This lets TUI renderers present version/license in distinct UI elements (e.g., a header panel).
- For ease of implementation, `ICommandOutput` may use **default interface method implementations** that delegate to `DefaultCommandOutput.Instance`, allowing custom handlers to override only the methods they need.

### 4.1.1 Diagnostic Payload on Exceptions

To support rich error rendering (e.g., re-printing the command line and underlining the exact option/argument/value that failed), parsing and validation errors attach a structured diagnostic payload to the thrown `CommandException`.

Proposed additive API on `CommandException` (and thus on all derived exceptions):

```csharp
public class CommandException : Exception
{
    public CommandDiagnostic? Diagnostic { get; init; }
}
```

This remains optional (`null` for exceptions thrown from user command actions unless the user sets it).

### 4.2 `CommandConfig.OutputFactory` Property

```csharp
public record CommandConfig()
{
    // ... existing members ...

    /// <summary>
    /// Gets a factory used to create the output handler that renders help, errors,
    /// version, and other user-visible output for a particular invocation.
    /// </summary>
    /// <remarks>
    /// The factory is passed the <see cref="CommandRunConfig"/> so an output implementation can
    /// configure itself from the effective stdout/stderr writers for that run.
    /// Return a singleton instance for stateless output handlers, or create a new instance per invocation.
    /// </remarks>
    public Func<CommandRunConfig, ICommandOutput>? OutputFactory { get; init; }
}
```

When `OutputFactory` is `null`, the library falls back to the current built-in plain-text rendering (preserving backward compatibility). Internally, this is handled by using a `DefaultCommandOutput` singleton.

### 4.3 `DefaultCommandOutput` Class

```csharp
/// <summary>
/// The default plain-text output handler that reproduces the library's built-in
/// help and error formatting. This class is sealed and cannot be inherited.
/// </summary>
/// <remarks>
/// This implementation writes to the <see cref="CommandRunConfig.Out"/> and
/// <see cref="CommandRunConfig.Error"/> streams using the same formatting as
/// previous versions of the library.
/// </remarks>
public sealed class DefaultCommandOutput : ICommandOutput
{
    /// <summary>
    /// Gets the singleton instance of the default output handler.
    /// </summary>
    public static readonly DefaultCommandOutput Instance = new();

    private DefaultCommandOutput() { }

    /// <inheritdoc />
    public void WriteHelp(Command command, CommandRunConfig runConfig) { /* current ShowHelp logic */ }

    /// <inheritdoc />
    public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception) { /* current WriteCommandException logic */ }

    /// <inheritdoc />
    public void WriteUnknownTokens(
        Command command,
        CommandRunConfig runConfig,
        UnknownTokenKind kind,
        IReadOnlyList<UnknownTokenInfo> unknownTokens) { /* current unknown-token rendering logic */ }

    /// <inheritdoc />
    public void WriteVersion(Command command, CommandRunConfig runConfig, string version) { /* current VersionOption logic */ }

    /// <inheritdoc />
    public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText) { /* current license header logic */ }
}
```

The existing rendering code from `Command.ShowHelp`, `WriteCommandException`, etc. is extracted into this class. The original methods become thin wrappers that delegate to the resolved output handler (`GetOutput(runConfig).WriteXxx(...)`).

### 4.4 Resolved Output Helper

An internal helper resolves the effective output handler:

```csharp
// Internal on Command:
internal ICommandOutput GetOutput(CommandRunConfig runConfig)
    => Config.OutputFactory?.Invoke(runConfig) ?? DefaultCommandOutput.Instance;
```

`OutputFactory` should be invoked **once per command invocation** (e.g., once per `RunAsync` call) and the resulting `ICommandOutput` instance should be reused for all output produced during that invocation (including sub-command dispatch). This allows outputs to allocate per-run resources (e.g., a TUI console wrapper) without repeated construction.

### 4.5 `CommandOutputHelper` Static Class

To assist custom `ICommandOutput` implementations, a public static helper class exposes the utility methods currently buried as private methods in `Command`:

```csharp
/// <summary>
/// Provides helper methods for building custom <see cref="ICommandOutput"/> implementations.
/// </summary>
public static class CommandOutputHelper
{
    /// <summary>
    /// Gets the full command path (e.g., "myexe commit push") for a command.
    /// </summary>
    public static string GetFullCommandPath(Command command)
        => command.GetFullCommandPath();

    /// <summary>
    /// Gets the default usage syntax string (e.g., "[options] &lt;command&gt;") for a command.
    /// </summary>
    public static string GetDefaultUsageSyntax(Command command)
        => command.GetDefaultUsageSyntax();

    /// <summary>
    /// Gets the display name for an option value placeholder from its description.
    /// For example, extracts "PORT" from the description "Server {PORT}".
    /// </summary>
    /// <param name="option">The option to get the argument name for.</param>
    /// <param name="valueIndex">The value index (0-based, for multi-value options).</param>
    /// <returns>The argument display name (e.g., "PORT", "VALUE").</returns>
    public static string GetOptionValueName(Option option, int valueIndex = 0);

    /// <summary>
    /// Gets only the description text from an option description, stripping
    /// argument-name placeholders (e.g., "{PORT}" becomes "PORT" inline).
    /// </summary>
    public static string GetDescriptionText(string? description);

    /// <summary>
    /// Gets the active, visible options for a command (deduplicated across aliases).
    /// </summary>
    public static IEnumerable<Option> GetVisibleOptions(Command command);

    /// <summary>
    /// Gets the active, visible positional arguments for a command.
    /// </summary>
    public static IEnumerable<CommandArgument> GetVisibleArguments(Command command);

    /// <summary>
    /// Gets the active, visible sub-commands for a command.
    /// </summary>
    public static IEnumerable<Command> GetVisibleSubCommands(Command command);

    /// <summary>
    /// Gets the active, visible argument sources for a command.
    /// </summary>
    public static IEnumerable<ArgumentSource> GetVisibleArgumentSources(Command command);

    /// <summary>
    /// Word-wraps a string to the specified width.
    /// </summary>
    public static IEnumerable<string> WordWrap(string text, int firstLineWidth, int remainingWidth);

    /// <summary>
    /// Formats the "Use `{commandPath} --help` for usage." hint message.
    /// </summary>
    public static string GetHelpHint(Command command);

    /// <summary>
    /// Formats a human-readable invocation line (command path + tokens) suitable for diagnostics.
    /// </summary>
    /// <remarks>
    /// This method applies minimal quoting so that tokens containing whitespace are readable.
    /// It is intended for error display only (not for shell round-tripping).
    /// </remarks>
    public static RenderedInvocation RenderInvocation(Command command, IReadOnlyList<string> tokens);

    /// <summary>
    /// Builds an underline marker string (e.g., carets) for a token span within a rendered invocation line.
    /// </summary>
    public static string RenderUnderline(RenderedInvocation invocation, CommandTokenSpan span, char marker = '^');
}

/// <summary>
/// Represents a formatted invocation along with token locations within the rendered text.
/// </summary>
/// <param name="Text">The rendered invocation text.</param>
/// <param name="TokenStarts">The 0-based start indices of each token within <paramref name="Text"/>.</param>
/// <param name="TokenLengths">The token lengths in characters.</param>
public readonly record struct RenderedInvocation(string Text, IReadOnlyList<int> TokenStarts, IReadOnlyList<int> TokenLengths);
```

These helpers let custom renderers reuse the library's parsing of description placeholders, visibility filtering, and word-wrapping logic without reimplementing them.

### 4.6 Per-Invocation Help Output

To enable scenarios like `--help-json` (or hosting the library in a UI that needs a different help representation), `Command` provides an overload to show help using a specific output implementation:

```csharp
public void ShowHelp(ICommandOutput output, CommandRunConfig? runConfig = null);
```

This overload routes the license header through `output.WriteLicenseHeader(...)` (if configured) before calling `output.WriteHelp(...)`.

---

## 5. Default Implementation Behavior

The `DefaultCommandOutput` class reproduces the exact output of the current implementation:

### 5.1 `WriteHelp`

When a license header is configured, the library calls `WriteLicenseHeader(...)` before calling `WriteHelp(...)`.

1. If no `CommandUsage` node is present, write the default usage line: `Usage: {fullPath} [options] <command>`.
2. Iterate `Command.Nodes` in declaration order:
   - **`CommandUsage`**: render its `Description` (with `{NAME}` and `{SYNTAX}` markers resolved).
   - **`Command`** (sub-command): render name + description in two-column layout.
   - **`Option`** (non-hidden, active): render `-x, --long=VALUE` prototype + description.
   - **`ArgumentSource`**: render its names + description.
   - **`CommandArgument`** (non-hidden, active): render `GetDisplayName()` + description.
   - **Other `ICommandNodeDescriptor`**: render description as a section header / paragraph.
3. Use `CommandRunConfig.Width` and `CommandRunConfig.OptionWidth` for column layout.
4. All strings pass through `CommandConfig.Localizer`.

### 5.2 `WriteError`

```
{fullCommandPath}: {exception.Message}
Use `{fullCommandPath} --help` for usage.
```

`WriteError` is used for `CommandException` failures that are not "unknown token" reports. Unknown tokens are routed through `WriteUnknownTokens` so suggestions and inactive-match notes can be rendered consistently.

### 5.3 `WriteUnknownTokens`

For each unknown token:

```
{fullCommandPath}: Unknown option: {token}
[Note: `{token}` matches an option that is currently inactive in this context.]
[Did you mean: {suggestion1}, {suggestion2}, {suggestion3}]
```

After reporting all tokens, the default implementation writes:

```
Use `{fullCommandPath} --help` for usage.
```

Called once per error report, with one or more `UnknownTokenInfo` entries. For `UnknownTokenKind.UnknownCommandOrOption`, the label is `Unknown command or option` instead of `Unknown option`.

### 5.4 `WriteVersion`

```
{version}
```

A single line to `runConfig.Out`.

### 5.5 `WriteLicenseHeader`

```
{licenseText}
```

A single line to `runConfig.Out`.

---

## 6. Usage Examples

### 6.1 Using the Default (No Change Required)

```csharp
// Existing code continues to work — OutputFactory defaults to null (built-in renderer).
var app = new CommandApp("myexe")
{
    new HelpOption(),
    { "n|name=", "Your {NAME}", v => name = v },
    (ctx, args) => ValueTask.FromResult(0),
};
await app.RunAsync(args);
```

### 6.2 Plugging a Custom Help Renderer

```csharp
var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = static runConfig => new TerminalUiCommandOutput(runConfig), // custom implementation
})
{
    new HelpOption(),
    { "n|name=", "Your {NAME}", v => name = v },
    { "p|port=", "Server {PORT}", (int v) => port = v },
    (ctx, args) => ValueTask.FromResult(0),
};
await app.RunAsync(args);
```

### 6.3 Implementing a Custom Renderer (Sketch)

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

public sealed class TerminalUiCommandOutput : ICommandOutput
{
    public TerminalUiCommandOutput(CommandRunConfig runConfig)
    {
        // Example strategy:
        // Initialize a terminal backend bound to the current invocation writers.
        //
        // Notes:
        // - This is an integration sketch; alternative hosting models are possible.
        // - If your app already initializes Terminal globally, you would not re-initialize here.
        Terminal.Initialize(
            backend: new VirtualTerminalBackend(outWriter: runConfig.Out, errorWriter: runConfig.Error),
            force: true);
    }

    public void WriteHelp(Command command, CommandRunConfig runConfig)
    {
        var header = new Group()
            .TopLeftText(command.GetFullCommandPath())
            .Padding(1)
            .Content(new Markup("[dim]Help[/]") { Wrap = false });

        var options = new Table()
            .Headers("Option", "Description");

        foreach (var option in CommandOutputHelper.GetVisibleOptions(command))
        {
            var names = string.Join(", ", option.Names.Select(
                n => n.Length == 1 ? $"-{n}" : $"--{n}"));
            var desc = CommandOutputHelper.GetDescriptionText(option.Description);
            options.AddRow(
                new Markup($"[green]{names}[/]") { Wrap = false },
                new TextBlock(desc ?? string.Empty));
        }

        Visual body = new VStack(header, options).Spacing(1);

        if (command.SubCommands.Count > 0)
        {
            var tree = new TreeView();
            var root = new TreeNode("Commands") { IsExpanded = true, Icon = TreeNodeIcons.FolderGlyph };
            foreach (var sub in CommandOutputHelper.GetVisibleSubCommands(command))
            {
                root.Children.Add(new TreeNode($"{sub.Name} — {sub.Description}") { Icon = TreeNodeIcons.DocumentGlyph });
            }
            tree.Roots.Add(root);
            body = new VStack(body, tree).Spacing(1);
        }

        Terminal.Write(body);
    }

    public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
    {
        Terminal.Write(
            new Group()
                .TopLeftText("Error")
                .Padding(1)
                .Content(new Markup($"[red]{exception.Message}[/]")));

        Terminal.Write(new Markup($"[dim]Use `{command.GetFullCommandPath()} --help` for usage.[/]") { Wrap = false });
    }

    public void WriteUnknownTokens(
        Command command,
        CommandRunConfig runConfig,
        UnknownTokenKind kind,
        IReadOnlyList<UnknownTokenInfo> unknownTokens)
    {
        var label = kind == UnknownTokenKind.UnknownCommandOrOption
            ? "Unknown command or option"
            : "Unknown option";

        foreach (var unknown in unknownTokens)
        {
            Terminal.Write(new Markup($"[red]{label}:[/] {unknown.Token}") { Wrap = false });

            if (unknown.InactiveMatchMessage is not null)
                Terminal.Write(new Markup($"[dim]{unknown.InactiveMatchMessage}[/]"));

            if (unknown.Suggestions.Count > 0)
                Terminal.Write(new Markup($"[yellow]Did you mean:[/] {string.Join(", ", unknown.Suggestions)}") { Wrap = false });
        }

        Terminal.Write(new Markup($"[dim]Use `{command.GetFullCommandPath()} --help` for usage.[/]") { Wrap = false });
    }

    public void WriteVersion(Command command, CommandRunConfig runConfig, string version)
    {
        Terminal.Write(new Markup(version) { Wrap = false });
    }

    public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText)
    {
        Terminal.Write(new Markup($"[dim]{licenseText}[/]"));
    }
}
```

### 6.4 Machine-Readable Help (JSON)

```csharp
public sealed class JsonCommandOutput : ICommandOutput
{
    public void WriteHelp(Command command, CommandRunConfig runConfig)
    {
        var model = new
        {
            command = command.GetFullCommandPath(),
            description = command.Description,
            options = CommandOutputHelper.GetVisibleOptions(command).Select(o => new
            {
                names = o.Names,
                description = CommandOutputHelper.GetDescriptionText(o.Description),
                required = o.OptionValueType == OptionValueType.Required,
                valueName = CommandOutputHelper.GetOptionValueName(o),
            }),
            arguments = CommandOutputHelper.GetVisibleArguments(command).Select(a => new
            {
                name = a.BasePrototype,
                description = a.Description,
                required = !a.Optional,
                list = a.IsList,
            }),
            subCommands = CommandOutputHelper.GetVisibleSubCommands(command).Select(s => new
            {
                name = s.Name,
                description = s.Description,
            }),
        };

        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
        runConfig.Out.WriteLine(json);
    }

    // ... other methods write JSON or no-op ...
}
```

### 6.5 Composing with the Default

A custom handler that only overrides help but keeps default error rendering:

```csharp
public sealed class CustomHelpOnlyOutput : ICommandOutput
{
    public void WriteHelp(Command command, CommandRunConfig runConfig)
    {
        // Custom rich help rendering...
    }

    // Delegate everything else to the built-in default:

    public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
        => DefaultCommandOutput.Instance.WriteError(command, runConfig, exception);

    public void WriteUnknownTokens(
        Command command,
        CommandRunConfig runConfig,
        UnknownTokenKind kind,
        IReadOnlyList<UnknownTokenInfo> unknownTokens)
        => DefaultCommandOutput.Instance.WriteUnknownTokens(command, runConfig, kind, unknownTokens);

    public void WriteVersion(Command command, CommandRunConfig runConfig, string version)
        => DefaultCommandOutput.Instance.WriteVersion(command, runConfig, version);

    public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText)
        => DefaultCommandOutput.Instance.WriteLicenseHeader(command, runConfig, licenseText);
}
```

### 6.6 Selecting Output Per Option (`--help-json`)

An option can render help using a different output implementation (without changing the app-wide default) by calling the `ShowHelp(ICommandOutput ...)` overload:

```csharp
public sealed class HelpJsonOption() : Option("help-json", "Show help as JSON")
{
    protected override void OnParseComplete(OptionContext c)
    {
        var commandContext = c.CommandRunContext;
        commandContext.ShouldRunAfterParsingOptions = false;

        c.Command.ShowHelp(new JsonCommandOutput(), commandContext.RunConfig);
    }
}
```

### 6.7 Rich Error Rendering (Underline the Failing Token)

Custom output renderers can inspect `CommandException.Diagnostic` to produce Rust-compiler-like error displays (re-print the invocation and underline the exact option/argument/value that failed).

```csharp
public sealed class UnderlineErrorOutput : ICommandOutput
{
    public void WriteHelp(Command command, CommandRunConfig runConfig)
        => DefaultCommandOutput.Instance.WriteHelp(command, runConfig);

    public void WriteUnknownTokens(
        Command command,
        CommandRunConfig runConfig,
        UnknownTokenKind kind,
        IReadOnlyList<UnknownTokenInfo> unknownTokens)
        => DefaultCommandOutput.Instance.WriteUnknownTokens(command, runConfig, kind, unknownTokens);

    public void WriteVersion(Command command, CommandRunConfig runConfig, string version)
        => DefaultCommandOutput.Instance.WriteVersion(command, runConfig, version);

    public void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText)
        => DefaultCommandOutput.Instance.WriteLicenseHeader(command, runConfig, licenseText);

    public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
    {
        if (exception.Diagnostic is CommandDiagnostic
            {
                Source: CommandDiagnosticSource.CommandLine,
                Tokens: not null,
                TokenSpan: not null
            } diag)
        {
            var invocation = CommandOutputHelper.RenderInvocation(command, diag.Tokens);
            runConfig.Error.WriteLine(invocation.Text);
            runConfig.Error.WriteLine(CommandOutputHelper.RenderUnderline(invocation, diag.TokenSpan.Value));
        }

        runConfig.Error.WriteLine($"{command.GetFullCommandPath()}: {exception.Message}");
        runConfig.Error.WriteLine(CommandOutputHelper.GetHelpHint(command));
    }
}
```

---

## 7. Integration with TUI / Rich Rendering Libraries

### 7.1 Why `Command` Is Sufficient as a Model

A custom `ICommandOutput` implementation receives the `Command` object, which already exposes all the structured data needed to build any visual representation:

{.table}
| Data | Access |
|---|---|
| Command name & path | `command.Name`, `command.GetFullCommandPath()` |
| Description | `command.Description` |
| Options (with aliases, value type, hidden flag) | `command.Options`, or `CommandOutputHelper.GetVisibleOptions(command)` |
| Positional arguments (with cardinality, display name) | `command.Arguments`, or `CommandOutputHelper.GetVisibleArguments(command)` |
| Sub-commands | `command.SubCommands`, or `CommandOutputHelper.GetVisibleSubCommands(command)` |
| Declaration order (for layout) | `command.Nodes` — preserves the order the user declared options, groups, arguments, headings |
| Conditional groups | `CommandGroup` nodes in `Nodes` with `IsActive()` |
| Usage line | `CommandUsage` nodes, or `CommandOutputHelper.GetDefaultUsageSyntax(command)` |
| Section headers / text blocks | `ICommandNodeDescriptor` nodes that are not options/args/commands |
| Argument sources (e.g., response files) | `ArgumentSource` subclasses in `Nodes` |

A TUI renderer walks `command.Nodes` in order to build its visual tree, using `is` pattern matching to identify node types:

```csharp
foreach (var node in command.Nodes)
{
    if (!node.IsActive()) continue;

    switch (node)
    {
        case CommandUsage usage:
            // render usage section
            break;
        case Command sub when !sub.Hidden:
            // add to sub-command tree/table
            break;
        case Option opt when !opt.Hidden:
            // add to options table
            break;
        case CommandArgument arg when !arg.Hidden:
            // add to arguments table
            break;
        case ArgumentSource src:
            // add to sources section
            break;
        case ICommandNodeDescriptor desc:
            // render as section header or text
            break;
    }
}
```

### 7.2 No Separate "Help Model" Class Needed

Because `Command` already provides a fully structured, read-only view of its contents, introducing a separate `CommandHelpModel` DTO would be redundant. The `CommandOutputHelper` static class bridges any gaps by exposing utility methods (visibility filtering, description parsing, word-wrapping) that would otherwise require reimplementation.

If a future scenario requires a serializable snapshot of the command structure (e.g., for caching or cross-process communication), a model class can be introduced as a separate additive feature without affecting `ICommandOutput`.

---

## 8. Interaction with Other Planned Features

### 8.1 Validation Errors (new-features-specs §2)

Validation errors throw `OptionException` or `CommandArgumentException`, which are subtypes of `CommandException`. They flow through `WriteError` naturally. A custom renderer can inspect the exception type:

```csharp
public void WriteError(Command command, CommandRunConfig runConfig, CommandException exception)
{
    if (exception is OptionException optEx)
    {
        // Render option-specific error with highlighting
    }
    else if (exception is CommandArgumentException argEx)
    {
        // Render argument-specific error
    }
    else
    {
        // Generic command error
    }
}
```

### 8.2 Environment Variable Fallback (new-features-specs §1)

When an env var fallback triggers a validation or parse error, the exception message includes `(from environment variable `{ENV_VAR_NAME}`)` and does not echo the environment variable value. This is part of the exception message string and flows through `WriteError` without any special handling from the output system.

Help output for env vars (e.g., `[env: MY_TOKEN]` suffix) is rendered by the `ICommandOutput.WriteHelp` implementation. The `DefaultCommandOutput` appends it automatically. Custom renderers access `Option.EnvironmentVariable` (the new property from the env var feature) to render the info however they choose.

### 8.3 Mutually Exclusive Options (new-features-specs §3)

Constraint violations produce `CommandException` (or a subtype) and flow through `WriteError`.

### 8.4 Test Helper API (new-features-specs §4)

`ParseResult` captures errors as `CommandException` objects in a list — it does not write output. The output handler is not involved during `Parse()`. It is only involved during `RunAsync()`. Rich diagnostics (`CommandException.Diagnostic`) remain useful in tests because they can be asserted without parsing stderr text.

### 8.5 Rich Diagnostics (Underline / Source Spans)

The output system supports "rich diagnostics" by attaching an optional `CommandDiagnostic` payload to `CommandException` (see §4.1.1). This payload is produced by the parser/validator and consumed by custom `ICommandOutput` implementations.

At minimum, the payload should support these cases:

- **Invalid option value** (typed parsing or validation): `Node = Option`, `Source = CommandLine` (or `EnvironmentVariable`), `TokenSpan` points to the failing value.
- **Invalid argument value**: `Node = CommandArgument`, `Source = CommandLine`, `TokenSpan` points to the failing positional value.
- **Missing required argument**: `Node = CommandArgument`, `Source = CommandLine`, `TokenSpan` can be a caret-at-end marker (implementation-defined).
- **Unknown tokens**: `UnknownTokenInfo.TokenSpan` can point to the unrecognized token so rich renderers can underline it.

This mechanism is intentionally presentation-focused: it exists to enable custom renderers (including XenoAtom.Terminal.UI-based UIs) to produce compiler-like messages. The default renderer must remain byte-identical to previous versions and should ignore this payload.

---

## 9. Implementation Notes

### 9.1 Refactoring Steps

The implementation is primarily a refactoring plus a small additive API surface (a new output interface + helpers, and one new config property):

1. **Create `ICommandOutput` interface** in a new file `ICommandOutput.cs` (along with `UnknownTokenKind` and `UnknownTokenInfo`).
2. **Create `DefaultCommandOutput` class** in a new file `DefaultCommandOutput.cs`.
   - Extract the body of `Command.ShowHelp` into `DefaultCommandOutput.WriteHelp`.
   - Extract `WriteCommandException` into `DefaultCommandOutput.WriteError`.
   - Extract `WriteUnknownCommandOrOption` and `WriteUnknownOptions` into `DefaultCommandOutput.WriteUnknownTokens`.
   - Extract version-writing logic into `DefaultCommandOutput.WriteVersion`.
   - Extract license-header writing into `DefaultCommandOutput.WriteLicenseHeader`.
3. **Create `CommandOutputHelper` static class** in a new file `CommandOutputHelper.cs`.
   - Promote relevant private helpers from `Command` (visibility filtering, description parsing, word-wrapping) to `public static` helpers.
4. **Add `OutputFactory` property** to `CommandConfig`.
5. **Add internal `GetOutput(runConfig)` helper** on `Command` to resolve `Config.OutputFactory?.Invoke(runConfig) ?? DefaultCommandOutput.Instance`.
6. **Update `Command.ShowHelp`** to delegate to `GetOutput(runConfig)` (including routing the license header through `WriteLicenseHeader`).
7. **Update `Command.RunAsync`** to delegate all library-produced output to `GetOutput(runConfig)` (unknown tokens, caught `CommandException`, and license header on run).
8. **Update `VersionOption.OnParseComplete`** to resolve the output handler and call `WriteVersion` instead of writing directly to `Out`.
9. **Populate rich diagnostics on parsing/validation errors** by attaching `CommandException.Diagnostic` (and `UnknownTokenInfo.TokenSpan` where applicable) from the parser/validator code paths. Ensure no secret values (e.g., env var contents) are included in diagnostics.

### 9.2 Internal Method Visibility

The private helper methods in `Command` that `DefaultCommandOutput` needs access to (e.g., `WriteOptionPrototype`, `WriteDescription`, `GetDefaultUsage`) should be promoted to `internal` (or moved to `CommandOutputHelper` as `public static`) so that `DefaultCommandOutput` can call them without duplication.

Methods that are useful for custom renderers become `public static` on `CommandOutputHelper`. Methods that are only needed by `DefaultCommandOutput` stay `internal`.

### 9.3 `VersionOption` Change

Today, `VersionOption.OnParseComplete` writes directly to `runConfig.Out`:

```csharp
protected override void OnParseComplete(OptionContext c)
{
    var commandContext = c.CommandRunContext;
    commandContext.ShouldRunAfterParsingOptions = false;
    commandContext.RunConfig.Out.WriteLine(Version);
}
```

This must change to route through the output handler. The simplest approach is to call the output handler directly from `OnParseComplete` (preserving current timing):

```csharp
// In VersionOption:
protected override void OnParseComplete(OptionContext c)
{
    var commandContext = c.CommandRunContext;
    commandContext.ShouldRunAfterParsingOptions = false;

    // Route through the configured output handler:
    c.Command.GetOutput(commandContext.RunConfig).WriteVersion(c.Command, commandContext.RunConfig, Version);
}
```

### 9.4 AOT / Trimming

- `ICommandOutput` is an interface with no default implementations requiring reflection.
- `DefaultCommandOutput` is a sealed class.
- `CommandOutputHelper` uses only static methods.
- No `System.Reflection` usage is introduced.
- The `JsonSerializer` usage in the JSON example (§6.4) is the consumer's choice and does not affect the library itself.

### 9.5 Thread Safety

`CommandConfig.OutputFactory` is shared across all commands in a `CommandApp` tree and may be invoked concurrently. Implementations should either:

- Return a **new** `ICommandOutput` instance per invocation (recommended for stateful renderers), or
- Return a singleton `ICommandOutput` that is stateless / thread-safe.

The `DefaultCommandOutput` singleton is inherently thread-safe (it has no mutable state; all state is passed via parameters).

---

## 10. Migration & Backward Compatibility

### 10.1 Source Compatibility

- **No breaking changes.** All existing public APIs remain unchanged.
- `Command.ShowHelp(CommandRunConfig?)` remains public and continues to work. It now delegates to the output handler internally.
- `CommandRunConfig` retains `Width`, `OptionWidth`, `Out`, `Error` — these are still used by `DefaultCommandOutput` and can be used by custom renderers.

### 10.2 Binary Compatibility

- No existing method signatures change.
- New types are additive: `UnknownTokenKind`, `UnknownTokenInfo`, `ICommandOutput`, `DefaultCommandOutput`, `CommandOutputHelper`.
- `CommandConfig` gains one new property (`OutputFactory`) with a default of `null`. Since `CommandConfig` is a `record`, this is binary-compatible (existing compiled code sees the default).
- `Command` gains a new overload `ShowHelp(ICommandOutput, CommandRunConfig?)` (additive).

### 10.3 Behavioral Compatibility

- When `CommandConfig.OutputFactory` is `null` (the default), the library produces byte-identical output to previous versions.
- Note: `CommandConfig` is a `record` — adding a new property may affect `ToString()` and record equality/hash code results.

---

## Documentation Updates

When implemented, the following documentation must be updated:

1. **XML doc comments** on all new public types and members.
2. **`doc/readme.md`** (user guide) — add a "Custom Output Rendering" section showing how to set `CommandConfig.OutputFactory` and implement `ICommandOutput`.
3. **`readme.md`** (project README) — mention pluggable output in the feature list.
4. **`doc/specs/new-features-specs.md`** — add a cross-reference to this spec in the error-formatting sections.

## Test Coverage

1. **Default output unchanged** — snapshot/golden-file tests comparing `DefaultCommandOutput` output against the current output for a representative set of commands.
2. **Custom output invoked** — tests verifying that setting `CommandConfig.OutputFactory` causes all output methods to be called on the custom handler.
3. **WriteHelp receives correct data** — tests verifying the `Command` passed to `WriteHelp` has the expected options, arguments, sub-commands.
4. **WriteError receives correct exception** — tests for `OptionException`, `CommandArgumentException`, generic `CommandException`.
5. **WriteUnknownTokens receives suggestions** — tests verifying fuzzy-match suggestions are passed to the handler.
6. **WriteVersion invoked** — tests confirming `--version` flows through the output handler.
7. **Composition with default** — test that a partial custom handler can delegate to `DefaultCommandOutput.Instance`.

