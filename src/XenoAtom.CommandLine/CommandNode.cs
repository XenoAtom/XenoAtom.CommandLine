// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// Base class for a command and options.
/// </summary>
public abstract class CommandNode
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="active">A callback that indicates if this node is active.</param> 
    internal CommandNode(Func<bool>? active = null)
    {
        ActivePredicate = active ?? (static () => true);
    }

    /// <summary>
    /// Gets the callback that indicates if this node is active. Default is true.
    /// </summary>
    public Func<bool> ActivePredicate { get; }

    /// <summary>
    /// Check if this node or any of its parent is inactive.
    /// </summary>
    /// <returns>true if the node is active; false otherwise</returns>
    public bool IsActive()
    {
        CommandNode? node = this;

        while (node != null)
        {
            if (!node.ActivePredicate())
            {
                return false;
            }

            node = node.Parent;
        }

        return true;
    }

    /// <summary>
    /// Gets the parent
    /// </summary>
    public CommandNode? Parent { get; internal set; }
}
