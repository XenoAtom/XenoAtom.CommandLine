// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.CommandLine;

/// <summary>
/// Validates a parsed option or argument value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="value">The parsed value to validate.</param>
/// <returns>An error message, or null when valid.</returns>
public delegate string? OptionValidator<in T>(T value);

