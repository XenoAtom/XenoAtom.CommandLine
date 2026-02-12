---
discard: true
title: "XenoAtom.CommandLine.Terminal — Visual As Command Nodes (Specification)"
---

# XenoAtom.CommandLine.Terminal — Visual As Command Nodes (Specification)

**Version scope:** XenoAtom.CommandLine.Terminal (net10.0)  
**Date:** February 2026  
**Status:** Implemented (February 2026)  
**Related:** `doc/specs/terminal-visual-specs.md` (help model + markup/visual outputs)

---

## 1. Motivation

Today, command definitions can embed plain help text using the fluent/initializer syntax:

```csharp
new Command("tool")
{
    "Options:",
    { "n|name=", "Your {NAME}", _ => { } },
    new HelpOption(),
}
```

With the Terminal support package, help can be rendered either as:

- **Markup** output (colored text).
- **Visual** output (Terminal.UI `Visual` tree).

However, there is currently no way to embed a Terminal.UI `Visual` directly into a command definition, e.g.:

```csharp
new Command("tool")
{
    new TextFiglet("HelloWorld").Style(...),
    "Options:",
    ...
}
```

This spec defines a small extension enabling **inline visuals in the command tree**, so that:

- `TerminalVisualCommandOutput` integrates them into the generated help visual tree.
- `TerminalMarkupCommandOutput` renders them as text by writing the visual to the terminal.
- Other outputs can either ignore them or handle them explicitly.

---

## 2. Goals / Non-Goals

### 2.1 Goals

1. Allow adding Terminal.UI visuals in a `Command`/`CommandApp` initializer with **no wrapper syntax**.
2. Preserve **node order**: visuals render exactly where declared (similar to plain text nodes).
3. Integrate cleanly with existing rendering tiers:
   - Markup tier: `Terminal.Write(visual)` (textual rendering).
   - Visual tier: inline integration in the help visual tree.
4. Ensure the default text output (`DefaultCommandOutput`) can still render visuals correctly as a preformatted block (not as a visual tree).
5. Keep the base library dependency-free: `XenoAtom.CommandLine` must not reference Terminal/UI types.

### 2.2 Non-Goals

1. This does not attempt to intercept or style user action output (`ctx.Out`/`ctx.Error`).
2. This does not require live UI/dynamic update support (bindings/states may exist, but help rendering is a snapshot).

---

## 3. Proposed API Surface (Terminal Package)

### 3.1 `TerminalVisualNode`

Add a node type in `XenoAtom.CommandLine.Terminal`:

```csharp
namespace XenoAtom.CommandLine.Terminal;

internal sealed class TerminalVisualNode : CommandNode, ICommandNodeDescriptor
{
    public TerminalVisualNode(Visual visual, string? fallbackText = null, Func<bool>? active = null);

    public Visual Visual { get; }

    // Optional fallback for outputs that don't support inline visuals.
    public string? Description { get; }
}
```

Notes:

- `TerminalVisualNode` is a pure presentation node (no parsing behavior).
- `IsActive()` should be respected in all outputs. The node is active if itself and all parents are active.
- `Description` is optional fallback text for outputs that do not know how to render `Visual`.
- `TerminalVisualNode` should internally support a cached text rendering (see 4.3) so `DefaultCommandOutput` can render it without destroying whitespace.

#### Visibility decision

Most `CommandNode` implementations used by fluent syntax are not public (for example the internal text node behind `Add(string)`).

This spec recommends keeping `TerminalVisualNode` **internal** and exposing only public extension methods on `CommandContainer` (see 3.2). Benefits:

- Keeps the public API surface of `XenoAtom.CommandLine.Terminal` small and hard to misuse.
- Avoids users taking a dependency on a low-level node type that may evolve with the help model.
- Matches the established pattern in `XenoAtom.CommandLine` (e.g. text nodes).

If a future scenario requires users to explicitly construct a node (rare), a public wrapper can be added later without blocking the initializer-first workflow.

### 3.2 Fluent Initializer Support: `Add(Visual)`

Add an extension method in `XenoAtom.CommandLine.Terminal` so collection initializers can accept a `Visual`:

```csharp
public static class CommandTerminalNodeExtensions
{
    public static TCommand Add<TCommand, TVisual>(this TCommand command, TVisual visual)
        where TCommand : CommandContainer;
        where TVisual : Visual;

    // Optional overload for non-terminal fallback text:
    // Enables initializer syntax: { visual, "Fallback" }
    public static TCommand Add<TCommand, TVisual>(this TCommand command, TVisual visual, string fallbackText)
        where TCommand : CommandContainer;
        where TVisual : Visual;
}
```

Behavior:

- `Add(command, visual)` wraps the `Visual` into a `TerminalVisualNode` and adds it to `command.Nodes`.
- The node is inserted at the exact position where it appears in the initializer.
- `TVisual : Visual` avoids accidental string-to-visual coercion in collection initializers while preserving direct `Visual` syntax.

### 3.3 Minimal Core Change Required

Because `CommandNode` currently has an `internal` constructor, an extension package cannot introduce new node types without one of the following:

Preferred options (in order):

1. Add an `InternalsVisibleTo` in `XenoAtom.CommandLine` for `XenoAtom.CommandLine.Terminal`, allowing the terminal package to derive from `CommandNode` while keeping the constructor non-public for third parties.
2. Change `CommandNode` constructor visibility to `protected internal` so official and third-party extension packages can introduce node types.

This spec recommends option (1) to keep the core surface area constrained while enabling the official extension package.

---

## 4. Rendering Behavior

### 4.1 Markup Tier (`TerminalMarkupCommandOutput`)

When help is rendered through `TerminalMarkupCommandOutput`:

- Inline `TerminalVisualNode` entries are rendered by calling `Terminal.Write(node.Visual)` at their position in the help output stream.
- Visual rendering must not change subsequent help alignment rules; it is an inline block.
- The `Terminal.Write*` APIs are **lazily initialized**. The renderer must not require an explicit `TerminalSession` to be active for one-shot help rendering.
- `TerminalSession` remains a convenience for deterministic cleanup / backend overrides and is typically only needed for scenarios like fullscreen/alternate-screen UIs where terminal state must be restored reliably.

### 4.2 Visual Tier (`TerminalVisualCommandOutput` / `ToHelpVisual`)

When help is rendered through `TerminalVisualCommandOutput` or `Command.ToHelpVisual()`:

- `TerminalVisualNode.Visual` is inserted directly into the generated visual tree at the node position.
- If `TerminalVisualOutputOptions.Theme` is set, it applies to the entire help visual (including inline visuals), consistent with existing behavior.

### 4.3 Default / Custom Outputs

For `DefaultCommandOutput` and custom `ICommandOutput` implementations:

- Because `TerminalVisualNode` is internal, outputs outside the terminal package cannot pattern-match it directly.
  - Outputs that only look at `ICommandNodeDescriptor.Description` will either display the provided fallback text or ignore the node.
  - Outputs that want to render the visual as text should detect `IHelpPreformattedContent` and call `WriteTo(...)` (see below).
- `DefaultCommandOutput` must render the visual as a **preformatted text block** (no wrapping) without going through the help word-wrapping logic (which would corrupt FIGlet/box drawing output).

This requires a small core hook so a node can write its own preformatted help content.

Proposed core interface (in `XenoAtom.CommandLine`):

```csharp
public interface IHelpPreformattedContent
{
    void WriteTo(TextWriter writer, CommandRunConfig runConfig);
}
```

Proposed `DefaultCommandOutput` behavior:

- When iterating `command.Nodes`, if a node implements `IHelpPreformattedContent` and is active, call `WriteTo(runConfig.Out, runConfig)` and continue (no wrapping, no trimming).
- Otherwise, keep existing behavior.

Proposed `TerminalVisualNode` behavior:

- Implements `IHelpPreformattedContent`.
- Renders `Visual` to preformatted text in a **non-ANSI** mode suitable for the default output:
  - Preferred: render via a temporary in-memory terminal backend configured with restricted capabilities, then copy the captured output to `writer`.
    - Use `InMemoryTerminalBackend(size: new TerminalSize(runConfig.Width, ...), capabilities: new TerminalCapabilities { AnsiEnabled = false, ColorLevel = TerminalColorLevel.None, IsOutputRedirected = true, IsInputRedirected = true, ... })`.
    - Render once using a temporary terminal session (e.g. `using var session = Terminal.Open(backend, ..., force: true); session.Instance.Write(visual);`), similar in spirit to the `TerminalAppTestDriver` approach used in Terminal.UI tests (but without running a live/fullscreen app).
    - Post-process the captured text as needed (e.g. trim trailing spaces per line) and write it to `writer`.
  - If the global terminal is already initialized and must not be overridden, fall back to a pure snapshot-based export (e.g. `VisualSnapshotRenderer.Render(...)` + plain-text extraction) to avoid mutating global terminal state.
- The rendered output should be cached per width (e.g. `(runConfig.Width, runConfig.OptionWidth)` or just `runConfig.Width`) to avoid re-rendering if help is requested multiple times.

This spec treats fallback behavior as optional:

- `fallbackText` should be provided by callers when they want a predictable non-terminal representation.
- If `fallbackText` is not provided, `Description` may be `null` (so non-terminal outputs skip it), while `DefaultCommandOutput` still works via `IHelpPreformattedContent`.

---

## 5. Help Model Integration (Terminal Package Internals)

To avoid duplicating traversal logic, inline visuals should be integrated into the existing terminal help pipeline:

- Extend the internal help model to represent either:
  - a new `HelpLineKind.Visual`, or
  - a `HelpLine` variant carrying a `Visual`.
- `HelpModelBuilder` should emit a visual line for `TerminalVisualNode` when the node is active.
- Both help renderers consume the same model:
  - Markup writer: writes markup/text lines and calls `Terminal.Write(visual)` for visual lines.
  - Visual builder: adds the `Visual` directly to the `VStack`.

This keeps traversal/prototype formatting centralized (as in `terminal-visual-specs.md`).

---

## 6. Examples

### 6.1 Command With Figlet Header (Gradient Style)

```csharp
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Styling;

const string _ = "";

var gradientBrush = Brush.LinearGradient(
    new GradientPoint(0f, 0f),
    new GradientPoint(1f, 0f),
    [
        new GradientStop(0f, Colors.DodgerBlue),
        new GradientStop(0.5f, Colors.White),
        new GradientStop(1f, Colors.Orange),
    ],
    mixSpaceOverride: ColorMixSpace.Oklab);

var app = new CommandApp("myexe", config: new CommandConfig
{
    OutputFactory = _ => new TerminalVisualCommandOutput()
})
{
    new TextFiglet("HelloWorld")
        .Font(FigletPredefinedFont.Standard)
        .LetterSpacing(1)
        .TextAlignment(TextAlignment.Left)
        .Style(TextFigletStyle.Default with { ForegroundBrush = gradientBrush }),
    _,
    "Options:",
    { "n|name=", "Your {NAME}", _ => { } },
    new HelpOption(),
    (ctx, _) => ValueTask.FromResult(0)
};
```

Expected behavior:

- `--help` with `TerminalVisualCommandOutput`: figlet appears inline at the top of the help visual.
- `--help` with `TerminalMarkupCommandOutput`: figlet is rendered by writing the visual to the terminal at that position.
- `--help` with `DefaultCommandOutput`: figlet is rendered as a preformatted text block via `IHelpPreformattedContent`.

### 6.2 Fallback Text For Non-Terminal Outputs

```csharp
new Command("tool")
{
    { new TextFiglet("Tool"), "Tool" },
    "Options:",
    ...
}
```

Expected behavior:

- Terminal outputs render the visual.
- Plain outputs may at least print `Tool`.

---

## 7. Testing Strategy (Implementation Guidance)

When implementing:

1. Add terminal tests for markup and visual outputs verifying that:
   - Inline visuals appear in help output in the correct order.
   - Visual nodes do not break section grouping (headers ending with `:` still group subsequent rows).
2. Use small visuals like `TextBlock("Hello")` for stable assertions (avoid brittle figlet snapshots).
3. Add a `DefaultCommandOutput` test ensuring `IHelpPreformattedContent` nodes bypass wrapping and preserve whitespace.
4. Add a test for the fallback path (no terminal session): `Description` is used or the node is ignored (decide and document).

