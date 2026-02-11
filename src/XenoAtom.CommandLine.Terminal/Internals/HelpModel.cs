using System.Collections.Generic;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal enum HelpLineKind
{
    Usage,
    Text,
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

internal sealed record HelpLine(HelpLineKind Kind, string? Text = null, bool IsSectionHeader = false, HelpRow? Row = null);

internal sealed class HelpModel
{
    public HelpModel(IReadOnlyList<HelpLine> lines)
    {
        Lines = lines;
    }

    public IReadOnlyList<HelpLine> Lines { get; }
}
