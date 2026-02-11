using System;
using XenoAtom.CommandLine.Terminal.Internals;
using XenoAtom.Terminal.UI;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// An <see cref="ICommandOutput"/> that renders help as Terminal.UI visuals.
/// </summary>
public sealed class TerminalVisualCommandOutput : TerminalMarkupCommandOutput
{
    private readonly TerminalVisualOutputOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="TerminalVisualCommandOutput"/>.
    /// </summary>
    /// <param name="options">Optional output options.</param>
    public TerminalVisualCommandOutput(TerminalVisualOutputOptions? options = null)
        : base((options ?? new TerminalVisualOutputOptions()).Markup)
    {
        _options = options ?? new TerminalVisualOutputOptions();
    }

    /// <inheritdoc />
    public override void WriteHelp(Command command, CommandRunConfig runConfig)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);

        var visual = command.ToHelpVisual(_options.Help);
        if (_options.Theme is not null)
        {
            visual.SetStyle(_options.Theme);
        }

        XenoAtom.Terminal.Terminal.Write(visual);
    }
}
