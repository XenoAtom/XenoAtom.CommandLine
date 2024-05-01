// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// Context used when parsing an option.
/// </summary>
public class OptionContext
{
    /// <summary>
    /// Creates a new instance of <see cref="OptionContext"/>.
    /// </summary>
    /// <param name="commandRunContext">The command run context.</param>
    /// <param name="command">The associated command</param>
    public OptionContext(CommandRunContext commandRunContext, Command command)
    {
        ArgumentNullException.ThrowIfNull(commandRunContext);
        ArgumentNullException.ThrowIfNull(command);
        CommandRunContext = commandRunContext;
        Command = command;
        OptionValues = new OptionValueCollection(this);
    }

    /// <summary>
    /// Gets or sets the option being processed.
    /// </summary>
    public Option? Option { get; set; }

    /// <summary>
    /// Gets or sets the option name being processed.
    /// </summary>
    public string? OptionName { get; set; }

    /// <summary>
    /// Gets or sets the option index being processed.
    /// </summary>
    public int OptionIndex { get; set; }

    /// <summary>
    /// Gets the associated command.
    /// </summary>
    public Command Command { get; }

    /// <summary>
    /// Gets the option values being processed.
    /// </summary>
    public OptionValueCollection OptionValues { get; }

    /// <summary>
    /// Gets the command run context.
    /// </summary>
    public CommandRunContext CommandRunContext { get; }
}