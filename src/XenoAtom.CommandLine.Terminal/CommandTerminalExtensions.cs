// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
    /// Adds a Terminal.UI visual node to a command container.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="TVisual">Type of the visual node.</typeparam>
    /// <param name="command">The command container to append to.</param>
    /// <param name="visual">The visual to append.</param>
    /// <returns>The command container.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> or <paramref name="visual"/> is <see langword="null"/>.</exception>
    public static TCommand Add<TCommand, TVisual>(this TCommand command, TVisual visual)
        where TCommand : CommandContainer
        where TVisual : Visual
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(visual);

        command.Add(new TerminalVisualNode(visual));
        return command;
    }

    /// <summary>
    /// Adds a Terminal.UI visual node with fallback text to a command container.
    /// </summary>
    /// <typeparam name="TCommand">Type of the command container.</typeparam>
    /// <typeparam name="TVisual">Type of the visual node.</typeparam>
    /// <param name="command">The command container to append to.</param>
    /// <param name="visual">The visual to append.</param>
    /// <param name="fallbackText">Fallback text for outputs that don't render visuals.</param>
    /// <returns>The command container.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/>, <paramref name="visual"/>, or <paramref name="fallbackText"/> is <see langword="null"/>.</exception>
    public static TCommand Add<TCommand, TVisual>(this TCommand command, TVisual visual, string fallbackText)
        where TCommand : CommandContainer
        where TVisual : Visual
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(fallbackText);

        command.Add(new TerminalVisualNode(visual, fallbackText));
        return command;
    }

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
