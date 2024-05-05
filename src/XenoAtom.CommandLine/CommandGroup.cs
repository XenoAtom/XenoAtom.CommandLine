// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// A group of commands that will be inlined in the parent command.
/// </summary>
/// <param name="active">A function to determine if this group is active</param>
public class CommandGroup(Func<bool>? active = null) : CommandContainer(active)
{
}