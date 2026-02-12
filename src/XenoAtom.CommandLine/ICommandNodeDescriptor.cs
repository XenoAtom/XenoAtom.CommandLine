// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.CommandLine;

/// <summary>
/// Provides plain-text help description intent for a <see cref="CommandNode"/>.
/// </summary>
public interface ICommandNodeDescriptor
{
    /// <summary>
    /// Gets the plain help text associated with this node.
    /// </summary>
    /// <remarks>
    /// This text should represent semantic help content (for example command/option/argument descriptions)
    /// and should not contain renderer-specific markup.
    /// </remarks>
    string? Description { get; }
}
