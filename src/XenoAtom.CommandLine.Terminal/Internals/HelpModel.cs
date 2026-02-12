// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using XenoAtom.Terminal.UI;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal enum HelpLineKind
{
    Usage,
    Text,
    Visual,
    Row,
    FooterHint,
    Blank,
}

internal enum HelpRowKind
{
    Option,
    Argument,
    Command,
    Source,
}

internal sealed record HelpRow(HelpRowKind Kind, string Prototype, string Description);

internal sealed record HelpLine(HelpLineKind Kind, string? Text = null, bool IsSectionHeader = false, HelpRow? Row = null, Visual? Visual = null);

internal sealed class HelpModel
{
    public HelpModel(IReadOnlyList<HelpLine> lines)
    {
        Lines = lines;
    }

    public IReadOnlyList<HelpLine> Lines { get; }
}
