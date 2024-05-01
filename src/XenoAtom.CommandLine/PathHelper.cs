// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.IO;

namespace XenoAtom.CommandLine;

/// <summary>
/// Internal helper class to get the name of the current executable keeping the symbolic name if any (to support Busybox like behavior)
/// </summary>
internal static class PathHelper
{
    public static string? GetExeName(string? name)
    {
        if (name == null)
        {
            return null;
        }

        name = Path.GetFileName(name);
        if (OperatingSystem.IsWindows())
        {
            return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 4) : name;
        }

        return name;
    }
}