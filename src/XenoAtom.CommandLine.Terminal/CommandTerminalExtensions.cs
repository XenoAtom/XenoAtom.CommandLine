using System;
using XenoAtom.CommandLine.Terminal.Internals;
using XenoAtom.Terminal.UI;

namespace XenoAtom.CommandLine.Terminal;

/// <summary>
/// Extension methods for building Terminal.UI visuals from command definitions.
/// </summary>
public static class CommandTerminalExtensions
{
    /// <summary>
    /// Builds a help visual for the specified command.
    /// </summary>
    /// <param name="command">The command to visualize.</param>
    /// <param name="options">Optional visual generation options.</param>
    /// <returns>A visual that can be rendered with <c>Terminal.Write(visual)</c> or embedded in another UI tree.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is <see langword="null"/>.</exception>
    public static Visual ToHelpVisual(this Command command, TerminalVisualOutputOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        var effectiveOptions = options ?? new TerminalVisualOutputOptions();
        var model = HelpModelBuilder.Build(command, effectiveOptions.PreserveNodeOrder);
        return HelpVisualBuilder.Build(model, effectiveOptions);
    }
}
