using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// Configuration options for <see cref="TerminalVisualCommandOutput"/>.
/// </summary>
/// <remarks>
/// Inherits all markup rendering options from <see cref="TerminalMarkupOutputOptions"/>
/// and adds only visual-specific configuration.
/// </remarks>
public sealed record TerminalVisualOutputOptions : TerminalMarkupOutputOptions
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
    /// Gets an optional table style override.
    /// </summary>
    public TableStyle? TableStyleOverride { get; init; }

    /// <summary>
    /// Gets a value indicating whether section headers ending with <c>:</c> should render
    /// following rows inside a grouped rounded container.
    /// </summary>
    public bool UseSectionGroups { get; init; } = true;

    /// <summary>
    /// Gets the minimum width, in cells, applied to each section group.
    /// Use this to keep grouped sections visually aligned.
    /// </summary>
    public int SectionGroupMinWidth { get; init; }

    /// <summary>
    /// Gets the style applied to section groups when <see cref="UseSectionGroups"/> is enabled.
    /// </summary>
    public GroupStyle SectionGroupStyle { get; init; } = GroupStyle.Rounded;

    /// <summary>
    /// Gets a value indicating whether errors and unknown token reports should render inside a visual group.
    /// </summary>
    public bool UseErrorGroups { get; init; } = true;

    /// <summary>
    /// Gets the minimum width, in cells, applied to error groups.
    /// </summary>
    public int ErrorGroupMinWidth { get; init; }

    /// <summary>
    /// Gets the style applied to error groups when <see cref="UseErrorGroups"/> is enabled.
    /// </summary>
    public GroupStyle ErrorGroupStyle { get; init; } = GroupStyle.Rounded;

    /// <summary>
    /// Gets an optional theme to apply to generated help visuals.
    /// </summary>
    public Theme? Theme { get; init; }
}
