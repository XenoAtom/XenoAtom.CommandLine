using System;
using System.Collections.Generic;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// Configuration options for <see cref="TerminalMarkupCommandOutput"/>.
/// </summary>
public sealed record TerminalMarkupOutputOptions
{
    /// <summary>
    /// Gets a value indicating whether the output should use <c>Terminal.WindowWidth</c> for layout when available.
    /// </summary>
    public bool UseTerminalWindowWidth { get; init; } = true;

    /// <summary>
    /// Gets an optional explicit width override used for layout.
    /// </summary>
    public int? WidthOverride { get; init; }

    /// <summary>
    /// Gets the markup style for usage lines.
    /// </summary>
    public string UsageStyle { get; init; } = "[bold]";

    /// <summary>
    /// Gets the markup style for section headers.
    /// </summary>
    public string SectionHeaderStyle { get; init; } = "[bold]";

    /// <summary>
    /// Gets the markup style for option prototypes.
    /// </summary>
    public string OptionPrototypeStyle { get; init; } = "[bright-yellow]";

    /// <summary>
    /// Gets the markup style for argument prototypes.
    /// </summary>
    public string ArgumentPrototypeStyle { get; init; } = "[bright-yellow]";

    /// <summary>
    /// Gets the markup style for command names in command listings.
    /// </summary>
    public string CommandNameStyle { get; init; } = "[cyan]";

    /// <summary>
    /// Gets the markup style for descriptions.
    /// </summary>
    public string DescriptionStyle { get; init; } = "[/]";

    /// <summary>
    /// Gets the markup style for secondary text (hints, source context, etc).
    /// </summary>
    public string HintStyle { get; init; } = "[dim]";

    /// <summary>
    /// Gets the markup style for error headers.
    /// </summary>
    public string ErrorStyle { get; init; } = "[bold red]";

    /// <summary>
    /// Gets a value indicating whether diagnostics should include invocation underlining when available.
    /// </summary>
    public bool ShowDiagnosticUnderline { get; init; } = true;

    /// <summary>
    /// Gets an optional provider for the current invocation tokens, used by unknown-token rendering.
    /// </summary>
    /// <remarks>
    /// <see cref="ICommandOutput.WriteUnknownTokens"/> provides token spans but not the full invocation token list.
    /// This provider enables full invocation+underline rendering for unknown tokens.
    /// </remarks>
    public Func<IReadOnlyList<string>?>? InvocationTokensProvider { get; init; }
}
