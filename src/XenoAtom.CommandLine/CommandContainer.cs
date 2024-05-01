// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XenoAtom.CommandLine;

/// <summary>
/// A base class for a command container. Cannot be inherited directly.
/// </summary>
public abstract class CommandContainer : CommandNode, IEnumerable
{
    private readonly List<CommandNode> _nodes = new();

    internal CommandContainer(Func<bool>? active = null) : base(active)
    {
        Nodes = new ReadOnlyCollection<CommandNode>(_nodes);
    }

    /// <summary>
    /// Gets all the nodes of this container.
    /// </summary>
    public ReadOnlyCollection<CommandNode> Nodes { get; }

    /// <summary>
    /// Adds a new node to this container.
    /// </summary>
    /// <param name="node">The node to add to this container.</param>
    public void Add(CommandNode node)
    {
        AddImpl(node);
    }

    /// <summary>
    /// Adds a node to this command.
    /// </summary>
    /// <param name="node">The command node to add to this command.</param>
    protected virtual void AddImpl(CommandNode node)
    {
        var parent = node.Parent;

        if (parent is CommandGroup)
        {
            while (parent is CommandGroup)
            {
                parent = parent.Parent;
            }

            if (parent == this)
            {
                parent = null;
            }
        }

        if (parent != null)
        {
            throw new InvalidOperationException($"The node `{node}` is already attached to a parent `{parent}`");
        }

        node.Parent ??= this;
        _nodes.Add(node);

        if (node is CommandGroup group)
        {
            foreach (var subNode in group.Nodes)
            {
                Add(subNode);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _nodes.GetEnumerator();
    }
}