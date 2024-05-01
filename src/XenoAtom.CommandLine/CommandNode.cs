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
        IsThisNodeActive = active ?? (static () => true);
    }

    /// <summary>
    /// Gets the callback that indicates if this node is active. Default is true.
    /// </summary>
    public Func<bool> IsThisNodeActive { get; }

    /// <summary>
    /// Check if this node or any of its parent is inactive.
    /// </summary>
    /// <returns>true if the node is active; false otherwise</returns>
    public bool IsActive()
    {
        CommandNode? node = this;

        while (node != null)
        {
            CommandNode? nextNode;

            if (!node.IsThisNodeActive())
            {
                return false;
            }

            lock (node)
            {
                nextNode = node.Parent;
            }
            node = nextNode;
        }

        return true;
    }

    /// <summary>
    /// Gets the parent
    /// </summary>
    public CommandNode? Parent { get; internal set; }
}