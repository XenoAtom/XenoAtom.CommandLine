// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.CommandLine;

/// <summary>
/// Represents a help option with the following default aliases: -h, -?, --help
/// </summary>
public class HelpOption(string prototype = "h|?|help", string help = "Show this message and exit") : Option(prototype, help)
{
    /// <inheritdoc />
    protected override void OnParseComplete(OptionContext c)
    {
        c.CommandRunContext.ShouldShowHelp = true;
    }
}