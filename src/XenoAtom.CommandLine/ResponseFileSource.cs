// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace XenoAtom.CommandLine;

/// <summary>
/// A response file source for parsing arguments from a file.
/// </summary>
public class ResponseFileSource : ArgumentSource
{
    /// <inheritdoc />
    public override string[] GetNames() => ["@file"];

    /// <inheritdoc />
    public override string Description => "Read response file for more options.";

    /// <inheritdoc />
    public override bool TryGetArguments(string value, [NotNullWhen(true)] out IEnumerable<string>? replacement)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith('@'))
        {
            replacement = null;
            return false;
        }

        replacement = GetArgumentsFromFile(value.Substring(1));
        return true;
    }
}