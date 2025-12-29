// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// Represents a command usage.
/// </summary>
/// <param name="description"></param>
public class CommandUsage(string? description) : CommandNode, ICommandNodeDescriptor
{
    private readonly string? _description = description;

    /// <summary>
    /// Gets the marker used to replace the full path name of the command from the description.
    /// </summary>
    public const string NameMarker = "{NAME}";

    /// <summary>
    /// Gets the marker used to replace the default syntax of the command (options/arguments/subcommands) from the description.
    /// </summary>
    public const string SyntaxMarker = "{SYNTAX}";

    /// <summary>
    /// Creates a new instance of <see cref="CommandUsage"/>, the usage description will be automatically rendered.
    /// </summary>
    public CommandUsage() : this("Usage: {NAME} {SYNTAX}")
    {
    }

    /// <inheritdoc />
    public string? Description
    {
        get
        {
            if (Parent != null && _description != null)
            {
                var command = GetCommand();
                if (command != null)
                {
                    var result = _description;
                    result = ReplaceMarker(result, NameMarker, command.GetFullCommandPath());
                    result = ReplaceMarker(result, SyntaxMarker, command.GetDefaultUsageSyntax());
                    return result;
                }
            }

            return _description;
        }
    }
    
    private Command? GetCommand()
    {
        for (var c = (CommandNode)this; c != null; c = c.Parent)
        {
            if (c is Command command)
            {
                return command;
            }
        }

        return null;
    }

    private static string ReplaceMarker(string value, string marker, string replacement)
    {
        var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return value;

        return $"{value.Substring(0, index)}{replacement}{value.Substring(index + marker.Length)}";
    }
}
