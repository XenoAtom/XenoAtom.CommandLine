// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.CommandLine;

/// <summary>
/// Interface used to add a description to a <see cref="CommandNode"/>>.
/// </summary>
public interface ICommandNodeDescriptor
{
    /// <summary>
    /// Gets the description of this command node (option, command...).
    /// </summary>
    string? Description { get; }
}