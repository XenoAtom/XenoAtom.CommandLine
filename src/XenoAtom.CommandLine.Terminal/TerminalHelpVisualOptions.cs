using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// Controls how help visuals are generated from a <see cref="Command"/>.
/// </summary>
public sealed record TerminalHelpVisualOptions
{
    /// <summary>
    /// Gets a value indicating whether options are rendered with a table layout.
    /// </summary>
    public bool UseTableForOptions { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether arguments are rendered with a table layout.
    /// </summary>
    public bool UseTableForArguments { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether commands are rendered with a table layout.
    /// </summary>
    public bool UseTableForCommands { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether command nodes are rendered in declaration order.
    /// </summary>
    public bool PreserveNodeOrder { get; init; } = true;

    /// <summary>
    /// Gets the markup style for usage lines.
    /// </summary>
    public string UsageMarkupStyle { get; init; } = "[bold primary]";

    /// <summary>
    /// Gets the markup style for option prototypes.
    /// </summary>
    public string OptionMarkupStyle { get; init; } = "[accent]";

    /// <summary>
    /// Gets the markup style for argument prototypes.
    /// </summary>
    public string ArgumentMarkupStyle { get; init; } = "[accent]";

    /// <summary>
    /// Gets the markup style for command names.
    /// </summary>
    public string CommandNameMarkupStyle { get; init; } = "[accent]";

    /// <summary>
    /// Gets the markup style for dim/muted text.
    /// </summary>
    public string DimMarkupStyle { get; init; } = "[muted]";

    /// <summary>
    /// Gets an optional table style override.
    /// </summary>
    public TableStyle? TableStyleOverride { get; init; }
}
