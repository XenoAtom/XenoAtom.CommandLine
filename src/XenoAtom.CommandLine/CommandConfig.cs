// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// The configuration for a <see cref="CommandApp"/>>.
/// </summary>
public record CommandConfig()
{
    /// <summary>
    /// The default configuration.
    /// </summary>
    public static readonly CommandConfig Default = new();

    /// <summary>
    /// The localizer for this command line application.
    /// </summary>
    public Converter<string, string> Localizer { get; init; } = static s => s;
}