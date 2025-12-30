// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;

namespace XenoAtom.CommandLine;

/// <summary>
/// Provides completion candidates for a value being completed.
/// </summary>
/// <param name="index">
/// For options: the 0-based value index within the option (e.g. 0 for the first value, 1 for the second).
/// For positional arguments: the 0-based argument index (including repeated indices for list/remainder arguments).
/// </param>
/// <param name="valuePrefix">The partially typed value prefix (can be empty).</param>
public delegate IEnumerable<string> ValueCompleter(int index, string valuePrefix);

