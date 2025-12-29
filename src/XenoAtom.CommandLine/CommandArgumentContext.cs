// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// Context used when parsing a command argument.
/// </summary>
public class CommandArgumentContext
{
    /// <summary>
    /// Creates a new instance of <see cref="CommandArgumentContext"/>.
    /// </summary>
    /// <param name="commandRunContext">The command run context.</param>
    /// <param name="command">The associated command.</param>
    public CommandArgumentContext(CommandRunContext commandRunContext, Command command)
    {
        ArgumentNullException.ThrowIfNull(commandRunContext);
        ArgumentNullException.ThrowIfNull(command);
        CommandRunContext = commandRunContext;
        Command = command;
    }

    /// <summary>
    /// Gets the argument being processed.
    /// </summary>
    public CommandArgument? Argument { get; internal set; }

    /// <summary>
    /// Gets the argument value being processed.
    /// </summary>
    public string? ArgumentValue { get; internal set; }

    /// <summary>
    /// Gets the argument index being processed.
    /// </summary>
    public int ArgumentIndex { get; internal set; }

    /// <summary>
    /// Gets the associated command.
    /// </summary>
    public Command Command { get; }

    /// <summary>
    /// Gets the command run context.
    /// </summary>
    public CommandRunContext CommandRunContext { get; }
}

