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
    /// Creates a new instance of <see cref="CommandUsage"/>, the usage description will be automatically rendered.
    /// </summary>
    public CommandUsage() : this(null)
    {
    }

    /// <inheritdoc />
    public string? Description
    {
        get
        {
            if (Parent != null && _description != null)
            {
                var index = _description.IndexOf(NameMarker, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var fullCommandPath = GetFullCommandPath() ?? string.Empty;
                    return $"{_description.Substring(0, index)}{fullCommandPath}{_description.Substring(index + NameMarker.Length)}";
                }
            }

            return _description;
        }
    }
    
    private string? GetFullCommandPath()
    {
        for (var c = (CommandNode)this; c != null; c = c.Parent)
        {
            if (c is Command command)
            {
                return command.GetFullCommandPath();
            }
        }

        return null;
    }
}