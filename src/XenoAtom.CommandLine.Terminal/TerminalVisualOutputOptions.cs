using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// Configuration options for <see cref="TerminalVisualCommandOutput"/>.
/// </summary>
public sealed record TerminalVisualOutputOptions
{
    /// <summary>
    /// Gets options used by the inherited markup renderer for errors/version/license output.
    /// </summary>
    public TerminalMarkupOutputOptions Markup { get; init; } = new();

    /// <summary>
    /// Gets options used to build help visuals.
    /// </summary>
    public TerminalHelpVisualOptions Help { get; init; } = new();

    /// <summary>
    /// Gets an optional theme to apply to generated help visuals.
    /// </summary>
    public Theme? Theme { get; init; }
}
