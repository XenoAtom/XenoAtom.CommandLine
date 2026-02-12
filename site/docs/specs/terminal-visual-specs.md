---
discard: true
title: "XenoAtom.CommandLine.Terminal — Terminal & Terminal.UI Output Specification"
---

# XenoAtom.CommandLine.Terminal — Terminal & Terminal.UI Output Specification

**Version scope:** new extension package (alongside XenoAtom.CommandLine v2.x)  
**Date:** February 2026  
**Status:** Draft v3  
**Supersedes:** [terminal-visual-specs-v2.md](terminal-visual-specs-v2.md) (Draft v2), [terminal-visual-specs.md](terminal-visual-specs.md) (Draft v1)

**Key changes vs v2:**

- Clarifies a shared internal help model to avoid duplicating traversal/prototype formatting across tiers.
- Clarifies unknown-token messaging (`UnknownTokenKind`, `UnknownTokenInfo.InactiveMatchMessage`).
- Uses a single style property set across both tiers (Visual inherits Markup option names and values).
- Unifies visual and markup options by making `TerminalVisualOutputOptions` inherit `TerminalMarkupOutputOptions` and removing the separate `TerminalHelpVisualOptions` type.

---

## Table of Contents

1. [Motivation](#1-motivation)
2. [Goals and Non-Goals](#2-goals-and-non-goals)
3. [Package Layout and Dependencies](#3-package-layout-and-dependencies)
4. [Architecture Overview](#4-architecture-overview)
5. [API Surface](#5-api-surface)
6. [Help Rendering — Markup Tier](#6-help-rendering--markup-tier)
7. [Help Rendering — Visual Tier](#7-help-rendering--visual-tier)
8. [Error Rendering Model](#8-error-rendering-model)
9. [Width, Wrapping, and Layout](#9-width-wrapping-and-layout)
10. [Security: Markup Injection](#10-security-markup-injection)
11. [Testing Strategy](#11-testing-strategy)
12. [Compatibility, Lifetime, and Migration](#12-compatibility-lifetime-and-migration)
13. [Example Outputs](#13-example-outputs)

---

## 1. Motivation

XenoAtom.CommandLine provides pluggable output via `ICommandOutput` (configured through `CommandConfig.OutputFactory`) so that help, errors, version, and license output can be custom-rendered by external systems.

The built-in `DefaultCommandOutput` writes plain text to `CommandRunConfig.Out`/`Error`. This works everywhere but lacks visual polish: no color, no alignment aids beyond fixed-width columns, no structured layout controls.

This spec defines a new extension package — **`XenoAtom.CommandLine.Terminal`** — that provides two progressively richer rendering tiers:

{.table}
| Tier | Rendering backend | When to use |
|------|-------------------|-------------|
| **Markup** | `XenoAtom.Terminal` (`Terminal.WriteMarkup`, `Terminal.WriteAtomic`) | CLI tools wanting colored, styled help without pulling in a layout framework |
| **Visual** | `XenoAtom.Terminal.UI` (`Terminal.Write(Visual)`) | Tools wanting structured layout (tables, groups, rules) and the ability to embed help inside fullscreen apps |

Key scenarios:

- CLI tools that want **colored, styled help** without rewriting help logic.
- Tools that want **structured help visuals** (aligned tables for options, bordered groups for sections) that also work when embedded in a fullscreen `Terminal.Run(...)` app.
- Rich **diagnostic error display** using `CommandException.Diagnostic` — underline the failing token similar to compiler diagnostics (e.g. Rust's `rustc`, .NET's Roslyn).

---

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Purely additive extension package**: zero changes to `XenoAtom.CommandLine` public API required.
2. **Two rendering tiers** in a single package, with shared theming configuration.
3. **Visual builder API**: produce a `Visual` directly from a `Command` object for embedding in fullscreen apps (e.g. inside a `ScrollViewer`).
4. **Diagnostics-first errors**: leverage `CommandException.Diagnostic`, `CommandTokenSpan`, and `UnknownTokenInfo.TokenSpan` to render compiler-style invocation+underline displays.
5. **No dependency leaks**: core `XenoAtom.CommandLine` remains dependency-free; only this extension package references Terminal/UI.
6. **AOT-friendly**: no runtime reflection, no `dynamic`, no code-gen at runtime.

### 2.2 Non-Goals

1. **Not intercepting user command output**: `ctx.Out` / `ctx.Error` in user action delegates are unaffected.
2. **Not byte-identical to `DefaultCommandOutput`**: terminal outputs intentionally differ in layout/style.
3. **No new command parsing features**: this is presentation only.
4. **No new public API on `XenoAtom.CommandLine`**: everything lives in the extension package namespace.

---

## 3. Package Layout and Dependencies

### 3.1 Project Structure

```
src/
  XenoAtom.CommandLine.Terminal/
    XenoAtom.CommandLine.Terminal.csproj
    TerminalMarkupCommandOutput.cs
    TerminalVisualCommandOutput.cs
    TerminalMarkupOutputOptions.cs
    TerminalVisualOutputOptions.cs
    CommandTerminalExtensions.cs      # ToHelpVisual() extension method
    Internals/
      HelpModel.cs                   # Shared help intermediate model (no rendering)
      HelpModelBuilder.cs            # Builds HelpModel from Command.Nodes (single source of truth)
      PrototypeFormatter.cs          # Shared prototype/description formatting helpers
      HelpMarkupWriter.cs            # Shared help markup rendering logic
      HelpVisualBuilder.cs           # Builds Visual tree from Command
      ErrorMarkupWriter.cs           # Shared error markup rendering logic
      MarkupStyles.cs                # Default markup style constants
  XenoAtom.CommandLine.Terminal.Tests/
    XenoAtom.CommandLine.Terminal.Tests.csproj
    TerminalMarkupCommandOutputTests.cs
    TerminalVisualCommandOutputTests.cs
    HelpVisualBuilderTests.cs
    ErrorRenderingTests.cs
    MarkupInjectionTests.cs
```

### 3.2 Target Framework

- `net10.0` **only** — `XenoAtom.Terminal.UI` requires C# 14 extension members, which are `net10.0`-only.

### 3.3 Dependencies

{.table}
| Dependency | Version | Notes |
|------------|---------|-------|
| `XenoAtom.CommandLine` | project reference or `>= 2.0` | Provides `ICommandOutput`, `CommandOutputHelper`, etc. |
| `XenoAtom.Terminal.UI` | `>= 1.0` | Pulls `XenoAtom.Terminal` and `XenoAtom.Ansi` transitively |

### 3.4 csproj Skeleton

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
    <IsAotCompatible>true</IsAotCompatible>
    <GenerateDocumentationFile>True</GenerateDocumentationFile>
    <Description>Terminal and Terminal.UI output renderers for XenoAtom.CommandLine.</Description>
    <PackageTags>command line;terminal;ui;help;ansi;markup</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\XenoAtom.CommandLine\XenoAtom.CommandLine.csproj" />
    <PackageReference Include="XenoAtom.Terminal.UI" Version="1.0.*" />
  </ItemGroup>
</Project>
```

---

## 4. Architecture Overview

```
┌───────────────────────────────────────────────────────┐
│              XenoAtom.CommandLine (core)               │
│   ICommandOutput  ─  CommandOutputHelper               │
│   Command  ─  Option  ─  CommandArgument                │
│   CommandException  ─  CommandDiagnostic                │
└──────────────────────┬────────────────────────────────┘
                       │ implements ICommandOutput
┌──────────────────────▼────────────────────────────────┐
│       XenoAtom.CommandLine.Terminal (this package)      │
│                                                         │
│  Internals (shared)                                     │
│    └─ HelpModelBuilder + PrototypeFormatter              │
│         ├─ HelpMarkupWriter  (Tier 1 help)               │
│         └─ HelpVisualBuilder (Tier 2 help)               │
│    └─ ErrorMarkupWriter (errors + diagnostics)           │
│                                                         │
│  TerminalMarkupCommandOutput (Tier 1 — Markup)           │
│    └─ uses Terminal.WriteMarkup / WriteAtomic            │
│    └─ renders HelpModel via HelpMarkupWriter             │
│                                                         │
│  TerminalVisualCommandOutput (Tier 2 — Visual)               │
│    └─ uses Terminal.Write(Visual)                        │
│    └─ renders HelpModel via HelpVisualBuilder            │
│    └─ reuses ErrorMarkupWriter (same as Tier 1)          │
│                                                         │
│  CommandTerminalExtensions                               │
│    └─ Command.ToHelpVisual() → Visual                   │
│    └─ for embedding in fullscreen Terminal.UI apps       │
└─────────────────────────────┬─────────────────────────┘
                              │ uses
┌─────────────────────────────▼─────────────────────────┐
│  XenoAtom.Terminal          │  XenoAtom.Terminal.UI    │
│  Terminal.WriteMarkup(...)  │  Terminal.Write(Visual)  │
│  Terminal.WriteAtomic(...)  │  Group, Table, Grid,     │
│  AnsiMarkup.Escape(...)     │  Markup, Rule, VStack,   │
│  InMemoryTerminalBackend    │  VisualSnapshotRenderer  │
└─────────────────────────────┴─────────────────────────┘
```

### 4.1 Inheritance Between Tiers

`TerminalVisualCommandOutput` inherits from `TerminalMarkupCommandOutput` and overrides help rendering while reusing the base for shorter-form outputs:

{.table}
| ICommandOutput method | Markup tier (base) | Visual tier (override?) |
|---|---|---|
| `WriteHelp` | Colored text via `Terminal.WriteMarkup` | **Override** — builds `Visual` tree, calls `Terminal.Write(visual)` |
| `WriteError` | Colored diagnostic text | Reuse base (optionally override for richer layout) |
| `WriteUnknownTokens` | Colored error + suggestions | Reuse base |
| `WriteVersion` | `Terminal.WriteMarkupLine(...)` | Reuse base |
| `WriteLicenseHeader` | `Terminal.WriteMarkupLine(...)` | Reuse base |

Rationale: error/version/license outputs are short and text-centric; a full visual tree adds complexity without meaningful benefit. Help is the only method where structured layout (tables, groups) adds real value.

### 4.2 Shared Help Model (No Duplication)

To keep the implementation small and prevent divergence between tiers, **help traversal and formatting are defined once** and reused:

- `HelpModelBuilder` walks the command tree and produces a backend-agnostic `HelpModel` (single source of truth).
- `PrototypeFormatter` builds option/argument/command prototype strings (aliases, placeholders, env-var hints) using `CommandOutputHelper` for placeholder/description normalization.
- `HelpMarkupWriter` renders `HelpModel` to styled text (Terminal markup tier).
- `HelpVisualBuilder` renders `HelpModel` to a `Visual` tree (Terminal.UI tier).

This ensures:

- The Markup and Visual tiers show the same content and follow the same visibility/ordering rules.
- Complex formatting logic (prototypes, wrapping decisions, env-var suffixes) does not get duplicated.

---

## 5. API Surface

### 5.1 Markup Output — `TerminalMarkupCommandOutput`

```csharp
namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// An <see cref="ICommandOutput"/> that writes colored, styled output
/// using <c>XenoAtom.Terminal</c> markup.
/// </summary>
public class TerminalMarkupCommandOutput : ICommandOutput
{
    /// <summary>
    /// Initializes a new instance with optional configuration.
    /// </summary>
    public TerminalMarkupCommandOutput(TerminalMarkupOutputOptions? options = null);

    /// <inheritdoc />
    public virtual void WriteHelp(Command command, CommandRunConfig runConfig);

    /// <inheritdoc />
    public virtual void WriteError(Command command, CommandRunConfig runConfig, CommandException exception);

    /// <inheritdoc />
    public virtual void WriteUnknownTokens(Command command, CommandRunConfig runConfig,
        UnknownTokenKind kind, IReadOnlyList<UnknownTokenInfo> unknownTokens);

    /// <inheritdoc />
    public virtual void WriteVersion(Command command, CommandRunConfig runConfig, string version);

    /// <inheritdoc />
    public virtual void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText);
}
```

### 5.2 Markup Output Options

```csharp
namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// Configuration options for <see cref="TerminalMarkupCommandOutput"/>.
/// </summary>
public sealed record TerminalMarkupOutputOptions
{
    /// <summary>
    /// When true, uses <c>Terminal.WindowWidth</c> for layout;
    /// otherwise falls back to <c>CommandRunConfig.Width</c>.
    /// </summary>
    public bool UseTerminalWindowWidth { get; init; } = true;

    /// <summary>
    /// Optional explicit width override (takes precedence over both
    /// <c>Terminal.WindowWidth</c> and <c>CommandRunConfig.Width</c>).
    /// </summary>
    public int? WidthOverride { get; init; }

    /// <summary>
    /// Markup style for the usage line (e.g. "Usage: app [options]").
    /// </summary>
    public string UsageStyle { get; init; } = "[bold]";

    /// <summary>
    /// Markup style for section headers (e.g. "Options:", "Arguments:").
    /// </summary>
    public string SectionHeaderStyle { get; init; } = "[bold]";

    /// <summary>
    /// Markup style for option prototypes (e.g. "-n, --name=VALUE").
    /// </summary>
    public string OptionPrototypeStyle { get; init; } = "[cyan]";

    /// <summary>
    /// Markup style for argument prototypes (e.g. "&lt;files&gt;*").
    /// </summary>
    public string ArgumentPrototypeStyle { get; init; } = "[cyan]";

    /// <summary>
    /// Markup style for command names in sub-command listings.
    /// </summary>
    public string CommandNameStyle { get; init; } = "[cyan]";

    /// <summary>
    /// Markup style for descriptions.
    /// </summary>
    public string DescriptionStyle { get; init; } = "[/]";

    /// <summary>
    /// Markup style for dim/secondary text (e.g. env var hints, help hints).
    /// </summary>
    public string HintStyle { get; init; } = "[dim]";

    /// <summary>
    /// Markup style for error headers.
    /// </summary>
    public string ErrorStyle { get; init; } = "[bold red]";

    /// <summary>
    /// When true, print compiler-like invocation + underline when diagnostics are available.
    /// </summary>
    public bool ShowDiagnosticUnderline { get; init; } = true;

    /// <summary>
    /// Optional provider for the current invocation tokens, used to render
    /// full invocation lines in unknown-token errors.
    /// </summary>
    /// <remarks>
    /// <see cref="ICommandOutput.WriteUnknownTokens"/> provides only <c>UnknownTokenInfo.TokenSpan</c>,
    /// not the full token list. When this provider is set, the output can show the full invocation
    /// line with underlining.
    /// </remarks>
    public Func<IReadOnlyList<string>?>? InvocationTokensProvider { get; init; }
}
```

### 5.3 Visual Output — `TerminalVisualCommandOutput`

```csharp
namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// An <see cref="ICommandOutput"/> that renders help as <c>XenoAtom.Terminal.UI</c>
/// visuals using <c>Terminal.Write(Visual)</c>.
/// </summary>
/// <remarks>
/// Inherits from <see cref="TerminalMarkupCommandOutput"/>. Overrides <see cref="WriteHelp"/>
/// to produce a <c>Visual</c> tree, while reusing the base class for error, version,
/// and license output.
/// </remarks>
public sealed class TerminalVisualCommandOutput : TerminalMarkupCommandOutput
{
    /// <summary>
    /// Initializes a new instance with optional configuration.
    /// </summary>
    public TerminalVisualCommandOutput(TerminalVisualOutputOptions? options = null);

    /// <inheritdoc />
    public override void WriteHelp(Command command, CommandRunConfig runConfig);
}
```

### 5.4 Visual Output Options

```csharp
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// Configuration options for <see cref="TerminalVisualCommandOutput"/>.
/// </summary>
/// <remarks>
/// Inherits all markup styling and diagnostic options from
/// <see cref="TerminalMarkupOutputOptions"/>. The Visual tier uses the same style
/// property names (for example <c>UsageStyle</c>, <c>OptionPrototypeStyle</c>,
/// <c>DescriptionStyle</c>, <c>HintStyle</c>) rather than introducing visual-specific duplicates.
/// </remarks>
public sealed record TerminalVisualOutputOptions : TerminalMarkupOutputOptions
{
    /// <summary>
    /// When true, renders options in a <c>Table</c> with aligned columns.
    /// When false, uses inline <c>Markup</c> blocks (closer to default output).
    /// </summary>
    public bool UseTableForOptions { get; init; } = true;

    /// <summary>
    /// When true, renders sub-commands in a <c>Table</c>.
    /// </summary>
    public bool UseTableForCommands { get; init; } = true;

    /// <summary>
    /// When true, renders positional arguments in a <c>Table</c>.
    /// </summary>
    public bool UseTableForArguments { get; init; } = true;

    /// <summary>
    /// When true, iterates <c>Command.Nodes</c> in declaration order to preserve
    /// the user's intended help layout (interleaved text headers, groups, etc.).
    /// When false, builds structured sections (all Options, then Arguments, then Commands).
    /// </summary>
    public bool PreserveNodeOrder { get; init; } = true;

    /// <summary>
    /// Optional table style override.
    /// Defaults to a borderless, compact table style for clean option alignment.
    /// </summary>
    public TableStyle? TableStyleOverride { get; init; }

    /// <summary>
    /// Optional theme to apply to the visual subtree.
    /// Defaults to <c>null</c> (falls back to <c>Theme.Terminal</c> automatically).
    /// </summary>
    public Theme? Theme { get; init; }
}
```

### 5.5 Visual Builder Extension Method

```csharp
using XenoAtom.Terminal.UI;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// Extension methods for building Terminal.UI visuals from command-line objects.
/// </summary>
public static class CommandTerminalExtensions
{
    /// <summary>
    /// Builds a help <see cref="Visual"/> from the specified command.
    /// </summary>
    /// <param name="command">The command whose help to visualize.</param>
    /// <param name="options">Optional visual builder options.</param>
    /// <returns>A <see cref="Visual"/> that can be written with <c>Terminal.Write(visual)</c>
    /// or embedded in a fullscreen app.</returns>
    /// <example>
    /// <code>
    /// // Inline usage:
    /// Terminal.Write(command.ToHelpVisual());
    ///
    /// // Fullscreen usage:
    /// Terminal.Run(new ScrollViewer(command.ToHelpVisual()), ...);
    /// </code>
    /// </example>
    public static Visual ToHelpVisual(this Command command, TerminalVisualOutputOptions? options = null);
}
```

### 5.6 Wiring Into CommandLine

**Markup output (simple):**

```csharp
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;

var app = new CommandApp("myapp", config: new CommandConfig
{
    OutputFactory = _ => new TerminalMarkupCommandOutput()
});
```

**Visual output (rich):**

```csharp
var app = new CommandApp("myapp", config: new CommandConfig
{
    OutputFactory = _ => new TerminalVisualCommandOutput()
});
```

**With options:**

```csharp
var app = new CommandApp("myapp", config: new CommandConfig
{
    OutputFactory = _ => new TerminalVisualCommandOutput(new TerminalVisualOutputOptions
    {
        UseTableForOptions = true,
        OptionPrototypeStyle = "[green]",
        Theme = Theme.DefaultLight,
    })
});
```

**Per-invocation override:**

```csharp
command.ShowHelp(new TerminalMarkupCommandOutput());
```

**Standalone visual builder (for fullscreen apps):**

```csharp
using var session = Terminal.Open();
Terminal.Run(
    new ScrollViewer(myCommand.ToHelpVisual()).MaxHeight(30),
    onUpdate: () => /* ... */);
```

---

## 6. Help Rendering — Markup Tier

### 6.1 Source of Truth

Help rendering uses `HelpModelBuilder` to walk `Command.Nodes` in declaration order (matching `DefaultCommandOutput` behavior) and produce a `HelpModel`. The Markup tier renders this model via `HelpMarkupWriter` (and the Visual tier reuses the same model via `HelpVisualBuilder`). Data extraction uses `CommandOutputHelper` methods exclusively:

{.table}
| Data needed | Helper method |
|-------------|---------------|
| Full command path | `CommandOutputHelper.GetFullCommandPath(command)` |
| Default usage syntax | `CommandOutputHelper.GetDefaultUsageSyntax(command)` |
| Option value placeholder | `CommandOutputHelper.GetOptionValueName(option, index)` |
| Clean description text | `CommandOutputHelper.GetDescriptionText(description)` |
| Visible options | `CommandOutputHelper.GetVisibleOptions(command)` |
| Visible arguments | `CommandOutputHelper.GetVisibleArguments(command)` |
| Visible sub-commands | `CommandOutputHelper.GetVisibleSubCommands(command)` |
| Help hint | `CommandOutputHelper.GetHelpHint(command)` |

### 6.2 Markup Rendering Rules

All output goes through `Terminal.WriteAtomic(...)` to ensure serialized, thread-safe output.

#### 6.2.1 Usage Line

If `CommandUsage` nodes exist in `Command.Nodes`, render their expanded descriptions (with `{NAME}` and `{SYNTAX}` already substituted by the core library). Otherwise render:

```
[bold]Usage: myapp [options] <command>[/]
```

User text in usage descriptions is escaped via `AnsiMarkup.Escape(...)`.

#### 6.2.2 Section Headers (Text Nodes)

String nodes and `ICommandNodeDescriptor` text nodes (e.g. `"Options:"`, `"Arguments:"`) are rendered as:

```
[bold]Options:[/]
```

Empty string nodes (`""`) produce a blank line (consistent with `DefaultCommandOutput`).

#### 6.2.3 Options

Each option is rendered as a single line (or wrapped if the prototype exceeds `OptionWidth`):

```
  [cyan]-n, --name=VALUE[/]           This is a name
  [cyan]-a, --age=AGE[/]              Sets the AGE [dim][env: MY_AGE][/]
```

- **Prototype column**: styled with `OptionPrototypeStyle`, built from `Option.GetNames()`, `OptionValueType`, `MaxValueCount`, and `GetOptionValueName(...)`.
- **Description column**: plain text (escaped) from `GetDescriptionText(...)`, word-wrapped to fit the remaining width.
- **Environment variable suffix**: appended in `HintStyle` when `Option.EnvironmentVariable` is not null/whitespace.
- **Column alignment**: same `OptionWidth` concept as `CommandRunConfig`, padded with spaces. When prototype exceeds column width, description starts on the next line (matching `DefaultCommandOutput` overflow behavior).

#### 6.2.4 Arguments

```
  [cyan]<files>*[/]                   Input files
```

Prototype from `argument.GetDisplayName()`, description from `argument.Description`. Same column layout as options.

#### 6.2.5 Sub-commands

```
  [cyan]hello[/]                      This is a hello command
  [cyan]world[/]                      This is a world command
```

#### 6.2.6 Footer Hint

When the command has sub-commands, render the footer:

```
[dim]Run 'myapp [command] --help' for more information on a command.[/]
```

#### 6.2.7 Version and License

- `WriteVersion`: `Terminal.WriteMarkupLine(AnsiMarkup.Escape(version))`
- `WriteLicenseHeader`: `Terminal.WriteMarkupLine(AnsiMarkup.Escape(licenseText))`

---

## 7. Help Rendering — Visual Tier

### 7.1 Visual Tree Structure

The `HelpVisualBuilder` produces a `VStack` as the root visual. Sections are built by walking `Command.Nodes` in order:

```
VStack (root, Spacing = 1)
├── Markup: usage line(s)
├── Markup: "Options:" (section header, `SectionHeaderStyle`)
├── Table: options (borderless, 2 columns)
│   ├── Row: [Markup: -n, --name=VALUE] [Markup: This is a name]
│   └── Row: [Markup: -a, --age=AGE]    [Markup: Sets the AGE  [dim][env: MY_AGE][/]]
├── Markup: "Arguments:" (section header)
├── Table: arguments (borderless, 2 columns)
│   └── Row: [Markup: <files>*] [Markup: Input files]
├── Markup: "Available commands:" (section header)
├── Table: commands (borderless, 2 columns)
│   ├── Row: [Markup: hello] [Markup: This is a hello command]
│   └── Row: [Markup: world] [Markup: This is a world command]
└── Markup: footer hint (dim)
```

### 7.2 Node Walk Algorithm (PreserveNodeOrder = true)

`HelpModelBuilder` walks `Command.Nodes` sequentially and batches consecutive nodes of the same kind. `HelpVisualBuilder` then converts these batches into a `VStack` + `Table`/`Markup` visuals:

1. **Skip** nodes where `!node.IsActive()` or `node.Hidden == true`.
2. **`CommandUsage`** → render as `Markup` with `UsageStyle`. Multiple usage nodes produce multiple `Markup` lines.
3. **Text descriptor nodes** (string descriptors that are not options/arguments/commands):
   - Non-empty text → `Markup` with `SectionHeaderStyle` (section headers like `"Options:"`).
   - Empty text (`""`) → produces spacing in the `VStack`.
4. **Consecutive `Option` nodes** → batched into a single `Table` with 2 columns:
   - Column 1 (`Auto` width): `Markup` with `OptionPrototypeStyle` wrapping the prototype string.
   - Column 2 (`Star` width): `Markup` or `TextBlock` with description + optional env var hint in `HintStyle`.
5. **Consecutive `CommandArgument` nodes** → batched into a `Table` (same 2-column layout).
6. **Consecutive `Command` (sub-command) nodes** → batched into a `Table` with name + description.
7. **`ArgumentSource`** (e.g. `ResponseFileSource`) → rendered as a row in the current table batch (if adjacent to options) or standalone.
8. **`CommandGroup`** → children are already inlined by the core library into `Command.Nodes`; the group's activation status is respected via `IsActive()`. No special visual treatment needed.

When `PreserveNodeOrder == false`, the builder collects all options, arguments, and sub-commands via `CommandOutputHelper.GetVisible*()` and renders them in fixed sections.

### 7.3 Table Style

Default table style for option/argument/command listings:

- **No borders** — no row separators, no column separators, no outer border.
- **Padding**: 2 spaces between columns (left padding on column 1 for indent).
- Column 1: `Auto` width (sized to widest prototype).
- Column 2: `Star` width (fills remaining space), text wrapping enabled.

This produces clean, lightweight alignment without the visual weight of grid lines. When `TableStyleOverride` is provided, that style takes precedence.

### 7.4 Control Selection Rationale

{.table}
| Help element | Terminal.UI control | Rationale |
|---|---|---|
| Usage line | `Markup` | Inline styled text, no alignment needed (`UsageStyle`) |
| Section header | `Markup` | Simple inline rendering (`SectionHeaderStyle`) |
| Option/arg/cmd listing | `Table` (borderless) | Auto-aligned columns, handles variable-width prototypes naturally |
| Option prototype | `Markup` (inside `Table` cell) | Styled prototype text (from `OptionPrototypeStyle`) |
| Description text | `Markup` (inside `Table` cell) | Supports env var dim suffixes, wraps via control (`DescriptionStyle` + `HintStyle`) |
| Footer hint | `Markup` | Hint text, single line (`HintStyle`) |
| Root container | `VStack` | Vertical stacking with `Spacing(1)` for visual separation |

**Why `Table` over `Grid`?** `Table` provides built-in row management (`.AddRow(...)`) and auto-sizing, making it simpler for dynamic content. `Grid` requires explicit `.Cell(content, row, col)` calls and upfront row/column definitions. Since help listings are naturally row-oriented, `Table` is a better fit.

**Why not `Group` (bordered sections)?** By default, sections use simple text headers without borders to match the lightweight feel of CLI help. Header styling comes from `SectionHeaderStyle`. Users who want bordered sections can wrap the visual in a `Group` after calling `ToHelpVisual()`.

### 7.5 `ToHelpVisual()` Implementation

- `CommandTerminalExtensions.ToHelpVisual()` delegates to `HelpVisualBuilder.Build(command, options)`.
- The returned `Visual` is a standalone subtree (not attached to any tree) — safe for `Terminal.Write(visual)` or embedding in any `Panel`.
- `TerminalVisualCommandOutput.WriteHelp()` calls `command.ToHelpVisual(options)` and then `Terminal.Write(visual)`.

---

## 8. Error Rendering Model

### 8.1 Error Message (`WriteError`)

Both tiers produce equivalent output:

```
[bold red]Error:[/] missing required option: --name
[dim]  multi hello --age 25[/]
[red]                       ^^^^^[/]
[dim]Use `multi hello --help` for usage.[/]
```

Rendering steps:

1. **Error header**: `"Error: "` in `ErrorStyle` followed by escaped `exception.Message`.
2. **Diagnostic underline** (when `exception.Diagnostic` is present and `ShowDiagnosticUnderline == true`):
   a. If `Diagnostic.Tokens` is available → use `CommandOutputHelper.RenderInvocation(command, tokens)`.
   b. If `Diagnostic.TokenSpan` is available → use `CommandOutputHelper.RenderUnderline(invocation, tokenSpan)`.
   c. Invocation line in `HintStyle` (dim), underline carets in `ErrorStyle` (red).
3. **Suggestions** (for `OptionException` with known token details): `"Did you mean: --name"`.
4. **Help hint**: `CommandOutputHelper.GetHelpHint(command)` in `HintStyle`.

### 8.2 Unknown Tokens (`WriteUnknownTokens`)

For each `UnknownTokenInfo`:

```
[bold red]Error:[/] Unknown option: --verbos
[dim]Did you mean: --verbose[/]
```

Message selection:

- `UnknownTokenKind.UnknownOption` → `"Unknown option: {token}"`
- `UnknownTokenKind.UnknownCommandOrOption` → `"Unknown command or option: {token}"`

If `UnknownTokenInfo.InactiveMatchMessage` is not null/empty, render it as a secondary line in `HintStyle` before suggestions (for example: "This matches an inactive option; try `--advanced`").

When `InvocationTokensProvider` is configured and `TokenSpan` is available, include the full invocation line with underlining before the suggestions (same rendering rules as `WriteError`).

After all unknown tokens, render the help hint.

### 8.3 Diagnostic Source Context

When `Diagnostic.Source` is not `CommandLine` (e.g. `ResponseFile` or `EnvironmentVariable`), prepend the source context:

```
[bold red]Error[/] [dim](in response file 'args.txt'):[/] invalid value for --port
```

---

## 9. Width, Wrapping, and Layout

### 9.1 Width Resolution

Both tiers resolve width in this priority:

1. `TerminalMarkupOutputOptions.WidthOverride` — if set, always wins.
2. `Terminal.WindowWidth` — when `UseTerminalWindowWidth == true` (default).
3. `CommandRunConfig.Width` — fallback (default `80`).

### 9.2 Markup Tier Layout

Uses the same two-column layout as `DefaultCommandOutput`:

- Left column (prototype): fixed width, typically `CommandRunConfig.OptionWidth` (default 29).
- Right column (description): remaining width, word-wrapped.
- When a prototype exceeds the column width, the description starts on the next line (indented).

Output goes through `Terminal.WriteAtomic(...)` for atomic multi-line writes, preventing interleaving from other threads.

### 9.3 Visual Tier Layout

Delegates layout entirely to Terminal.UI's measure/arrange/render pipeline:

- `Table` auto-sizes the prototype column (column 1 = `Auto`) and fills the description column (column 2 = `Star`).
- `VStack` handles vertical stacking with configurable `Spacing(1)`.
- Word wrapping is handled by the `Markup` and `TextBlock` controls' built-in wrapping.
- `Terminal.Write(visual)` uses `Terminal.WindowWidth` to size the visual automatically.

When embedded in a fullscreen app, the visual adapts to the available width:

```csharp
Terminal.Run(new ScrollViewer(command.ToHelpVisual()), ...);
```

---

## 10. Security: Markup Injection

Error messages and unknown tokens frequently contain **user-provided text** (CLI tokens, file paths, etc.). When rendering with markup:

- **Always escape** user-provided fragments via `AnsiMarkup.Escape(value)` before embedding in markup strings.
- For Terminal.UI `Markup` controls: use escape on all user-provided text interpolated into markup strings.
- For `TextBlock`: renders plain text, no markup injection risk.

Specific escaping points:

{.table}
| Method | User text source | Escaping |
|--------|-----------------|----------|
| `WriteError` | `exception.Message` | `AnsiMarkup.Escape(...)` |
| `WriteError` | invocation text built from `exception.Diagnostic.Tokens` | escape tokens when embedding into markup |
| `WriteUnknownTokens` | `unknownToken.Token`, suggestions | `AnsiMarkup.Escape(...)` |
| `WriteUnknownTokens` | invocation text built from `InvocationTokensProvider` | escape tokens when embedding into markup |
| `WriteVersion` | version string | `AnsiMarkup.Escape(...)` |
| `WriteLicenseHeader` | license text | `AnsiMarkup.Escape(...)` |
| `WriteHelp` | option/argument descriptions | `AnsiMarkup.Escape(...)` |
| `WriteHelp` | `CommandUsage` descriptions | `AnsiMarkup.Escape(...)` |
| `WriteHelp` | environment variable names | `AnsiMarkup.Escape(...)` |

Never include secret values (env var *values*) in diagnostics — already enforced by the core library.

---

## 11. Testing Strategy

### 11.1 Markup Output Tests

Use `XenoAtom.Terminal`'s **`InMemoryTerminalBackend`** to capture output deterministically:

```csharp
var backend = new InMemoryTerminalBackend();
Terminal.Initialize(backend);

var app = new CommandApp("testapp", config: new CommandConfig
{
    OutputFactory = _ => new TerminalMarkupCommandOutput(new TerminalMarkupOutputOptions
    {
        UseTerminalWindowWidth = false,  // use CommandRunConfig.Width for deterministic output
    })
})
{
    new CommandUsage(),
    "Options:",
    { "n|name=", "The {NAME}", v => { } },
    { "v|verbose", "Enable verbose output", v => { } },
    new HelpOption(),
};

await app.RunAsync(["--help"], new CommandRunConfig(Width: 80));

var output = backend.GetOutText();
// Assert output contains expected ANSI-styled text
```

Key test cases:

- Correct ANSI escape sequences for styled prototypes.
- Proper word-wrapping at column boundaries.
- Environment variable hints formatted with dim style.
- Diagnostic underlines in error output.
- No unescaped user text in markup positions (injection test).

### 11.2 Visual Builder Tests

Build visuals and render offscreen via `VisualSnapshotRenderer`:

```csharp
var command = new Command("test")
{
    "Options:",
    { "n|name=", "The name", v => { } },
    { "v|verbose", "Verbose", v => { } },
    new HelpOption(),
};

var visual = command.ToHelpVisual(new TerminalVisualOutputOptions
{
    UseTableForOptions = true,
});

var buffer = VisualSnapshotRenderer.Render(visual, width: 80, maxHeight: 200, theme: Theme.Terminal);
// Convert CellBuffer to text and assert key content
```

Key assertions:

- Table columns are aligned correctly.
- All visible options, arguments, and sub-commands are present.
- Hidden/inactive nodes are omitted.
- Section headers appear in the correct position relative to their sections.
- `CommandGroup` nodes with inactive predicates are excluded.

### 11.3 Integration Tests

Full end-to-end tests exercising the complete pipeline:

- Build a `CommandApp` with various option types (required, optional, multi-value, key-value), sub-commands, and conditional groups.
- Run with `--help`, capture output, assert expected structure.
- Run with invalid options, capture error output, assert diagnostic underlines.
- Run with unknown tokens, capture error output, assert suggestions.
- Run with `--advanced --help` to test conditional group visibility.

### 11.4 Markup Injection Tests

Ensure user-provided text containing markup-like characters is properly escaped:

```csharp
var app = new CommandApp("test", config: new CommandConfig
{
    OutputFactory = _ => new TerminalMarkupCommandOutput()
})
{
    { "x=", "Option with [brackets] in description", v => { } },
};

// Run with help and verify [brackets] in description doesn't break markup
await app.RunAsync(["--help"]);
var output = backend.GetOutText();
// Assert that "[brackets]" appears literally, not as a markup tag
```

---

## 12. Compatibility, Lifetime, and Migration

### 12.1 Opt-In Only

This package is **purely additive**. Consumers opt-in by:

1. Adding a `PackageReference` to `XenoAtom.CommandLine.Terminal`.
2. Configuring `CommandConfig.OutputFactory` to return one of the new output implementations.

No changes to existing apps are required unless they want terminal visuals.

### 12.2 Lifetime Notes

`XenoAtom.CommandLine` creates the output handler via `CommandConfig.OutputFactory` and does not dispose it.

Implications:

- Output implementations must be effectively **stateless** and must not require disposal.
- Do **not** call `Terminal.Open()` inside the output implementation — callers should manage their own `TerminalSession` externally (at process startup).
- Both `TerminalMarkupCommandOutput` and `TerminalVisualCommandOutput` write to the ambient `Terminal` instance, which must already be initialized by the time output methods are called.

Typical caller pattern:

```csharp
using var session = Terminal.Open();

var app = new CommandApp("myapp", config: new CommandConfig
{
    OutputFactory = _ => new TerminalVisualCommandOutput()
});

return await app.RunAsync(args);
```

### 12.3 Platform Constraints

- **`net10.0` only** — `XenoAtom.Terminal.UI` requires C# 14 extension members.
- Terminal must support ANSI escape sequences for colored output. On terminals without ANSI support, output degrades gracefully (handled by `XenoAtom.Terminal` automatically).
- **NativeAOT compatible** — no reflection, no `dynamic`, all types are `sealed` or concrete.

### 12.4 Migration From `DefaultCommandOutput`

For users currently using the default output:

```diff
+ using XenoAtom.CommandLine.Terminal;
+ using XenoAtom.Terminal;

+ using var session = Terminal.Open();

  var app = new CommandApp("myapp", config: new CommandConfig
  {
+     OutputFactory = _ => new TerminalVisualCommandOutput()
  });

  return await app.RunAsync(args);
```

The help layout will change (styled text, auto-aligned tables instead of fixed-width columns), but the information content is identical.

---

## 13. Example Outputs

### 13.1 Markup Tier — `multi --help`

With ANSI styling applied (represented here with markdown formatting):

```
Usage: multi [options] <command>          ← bold

Options:                                  ← bold
  -D[=NAME:VALUE]            Add a ...   ← prototype in cyan
  -f=FILE                    The input…
  -x                         Extract …
  -c                         Create …
  -t                         List …
  -a, --advanced             Show adv…   ← cyan
  -h, -?, --help             Show thi…   ← cyan
  -v, --version              Show the…

Available commands:                       ← bold
  hello                      This is …   ← name in cyan
  world                      This is …

Run 'multi [command] --help' for …        ← dim
```

### 13.2 Visual Tier — `multi --help`

Rendered using `Terminal.Write(Visual)` with borderless `Table` controls:

```
Usage: multi [options] <command>          ← Markup (`UsageStyle`)

Options:                                  ← Markup (`SectionHeaderStyle`)
  -D[=NAME:VALUE]   Add a marco NAME…    ← Table row, prototype styled (accent)
  -f=FILE            The input FILE       ← auto-aligned columns
  -x                 Extract the file
  -c                 Create the file
  -t                 List the file
  -a, --advanced     Show advanced options
  -h, -?, --help     Show this message…
  -v, --version      Show the version…

Available commands:                       ← Markup (`SectionHeaderStyle`)
  hello              This is a hello…    ← Table row, name styled (accent)
  world              This is a world…

Run 'multi [command] --help' for …        ← Markup, dim
```

Note: column widths are auto-calculated by the `Table` control rather than using a fixed `OptionWidth`, resulting in tighter alignment.

### 13.3 Visual Tier — `multi hello --help`

```
Usage: multi hello [options] <files>*

Options:
  -n, --name=VALUE   This is a name
  -a, --age=AGE      Sets the AGE
  -h, -?, --help     Show this message and exit

Arguments:
  <files>*           Input files
```

### 13.4 Error Output — Both Tiers

```
Error: missing required option: --name     ← "Error:" bold red, message in default
  multi hello --age 25                     ← dim (invocation line)
                     ^^                    ← red (underline carets)
Use `multi hello --help` for usage.        ← dim
```

### 13.5 Unknown Token Error

```
Error: Unknown option: --verbos            ← "Error:" bold red (message depends on UnknownTokenKind)
This matches an inactive option; try `--advanced`.  ← dim (InactiveMatchMessage, optional)
Did you mean: --verbose                    ← suggestions (optional)
Use `multi --help` for usage.              ← dim
```

### 13.6 Fullscreen Embedding

```csharp
using var session = Terminal.Open();

var helpVisual = myCommand.ToHelpVisual(new TerminalVisualOutputOptions
{
    UseTableForOptions = true,
    OptionPrototypeStyle = "[green]",
});

Terminal.Run(
    new VStack(
        new Header("My Application"),
        new ScrollViewer(helpVisual).Stretch(),
        new Footer("Press Q to quit")
    ),
    onUpdate: () => /* handle input */);
```

