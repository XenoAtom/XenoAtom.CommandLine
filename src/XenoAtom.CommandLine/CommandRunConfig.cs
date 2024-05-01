// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.IO;

namespace XenoAtom.CommandLine;

/// <summary>
/// Configuration for running a command.
/// </summary>
/// <param name="Width">The maximum width of the text for displaying the help message.</param>
/// <param name="OptionWidth">The maximum width of the text for displaying the options.</param>
public record CommandRunConfig(int Width = 80, int OptionWidth = 29)
{
    /// <summary>
    /// Gets or sets a boolean indicating if the license should be displayed when running the command.
    /// </summary>
    public bool ShowLicenseOnRun { get; init; } = true;

    /// <summary>
    /// Gets or sets the output stream for the command.
    /// </summary>
    public TextWriter Out { get; init; } = Console.Out;

    /// <summary>
    /// Gets or sets the error stream for the command.
    /// </summary>
    public TextWriter Error { get; init; } = Console.Error;

    internal readonly int DescriptionFirstWidth = Width - OptionWidth;

    internal readonly int DescriptionRemWidth = Width - OptionWidth - 2;

    internal readonly string CommandHelpIndentStart = new string(' ', OptionWidth);

    internal readonly string CommandHelpIndentRemaining = new string(' ', OptionWidth + 2);
}