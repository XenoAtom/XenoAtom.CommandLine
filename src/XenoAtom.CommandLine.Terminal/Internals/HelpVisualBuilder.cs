using System;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal static class HelpVisualBuilder
{
    public static Visual Build(HelpModel model, TerminalVisualOutputOptions options)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(options);

        var root = new VStack();

        for (var index = 0; index < model.Lines.Count; index++)
        {
            var line = model.Lines[index];
            switch (line.Kind)
            {
                case HelpLineKind.Blank:
                    root.Add(new TextBlock(string.Empty));
                    continue;

                case HelpLineKind.Usage:
                    root.Add(new Markup(MarkupStyleHelper.ApplyStyle(options.UsageStyle, line.Text ?? string.Empty)));
                    continue;

                case HelpLineKind.Text:
                    if (line.IsSectionHeader && TryReadGroupedSection(model, index, options, out var sectionGroup, out var sectionEnd))
                    {
                        root.Add(sectionGroup);
                        index = sectionEnd;
                        continue;
                    }

                    if (string.IsNullOrEmpty(line.Text))
                    {
                        root.Add(new TextBlock(string.Empty));
                    }
                    else
                    {
                        var style = line.IsSectionHeader ? options.SectionHeaderStyle : options.DescriptionStyle;
                        root.Add(new Markup(MarkupStyleHelper.ApplyStyle(style, line.Text)));
                    }
                    continue;

                case HelpLineKind.FooterHint:
                    root.Add(new Markup(MarkupStyleHelper.ApplyStyle(options.HintStyle, line.Text ?? string.Empty)));
                    continue;

                case HelpLineKind.Row when line.Row is not null:
                    index = AddRows(root, model, index, options);
                    continue;
            }
        }

        return root;
    }

    private static bool TryReadGroupedSection(HelpModel model, int headerIndex, TerminalVisualOutputOptions options, out Visual sectionGroup, out int sectionEnd)
    {
        sectionGroup = null!;
        sectionEnd = headerIndex;

        if (!options.UseSectionGroups)
        {
            return false;
        }

        var headerText = model.Lines[headerIndex].Text;
        if (!IsGroupHeader(headerText))
        {
            return false;
        }

        var start = headerIndex + 1;
        if (start >= model.Lines.Count)
        {
            return false;
        }

        var end = start - 1;
        var hasRow = false;
        for (var index = start; index < model.Lines.Count; index++)
        {
            var line = model.Lines[index];
            if (line.Kind is HelpLineKind.FooterHint or HelpLineKind.Usage)
            {
                break;
            }

            if (line.Kind == HelpLineKind.Text && line.IsSectionHeader && IsGroupHeader(line.Text))
            {
                break;
            }

            if (line.Kind == HelpLineKind.Row && line.Row is not null)
            {
                hasRow = true;
            }

            end = index;
        }

        while (end >= start && model.Lines[end].Kind == HelpLineKind.Blank)
        {
            end--;
        }

        if (!hasRow || end < start)
        {
            return false;
        }

        var content = new VStack();
        for (var index = start; index <= end; index++)
        {
            var line = model.Lines[index];
            switch (line.Kind)
            {
                case HelpLineKind.Blank:
                    content.Add(new TextBlock(string.Empty));
                    break;

                case HelpLineKind.Usage:
                    content.Add(new Markup(MarkupStyleHelper.ApplyStyle(options.UsageStyle, line.Text ?? string.Empty)));
                    break;

                case HelpLineKind.Text:
                    if (string.IsNullOrEmpty(line.Text))
                    {
                        content.Add(new TextBlock(string.Empty));
                    }
                    else
                    {
                        var style = line.IsSectionHeader ? options.SectionHeaderStyle : options.DescriptionStyle;
                        content.Add(new Markup(MarkupStyleHelper.ApplyStyle(style, line.Text)));
                    }
                    break;

                case HelpLineKind.FooterHint:
                    content.Add(new Markup(MarkupStyleHelper.ApplyStyle(options.HintStyle, line.Text ?? string.Empty)));
                    break;

                case HelpLineKind.Row when line.Row is not null:
                    index = AddRows(content, model, index, options);
                    break;
            }
        }

        var title = GetGroupTitle(headerText!);
        var groupTitle = new Markup(MarkupStyleHelper.ApplyStyle(options.SectionHeaderStyle, title));
        var group = new Group(groupTitle, content);
        if (options.SectionGroupMinWidth > 0)
        {
            group.MinWidth = options.SectionGroupMinWidth;
        }
        group.Style(options.SectionGroupStyle);

        sectionGroup = group;
        sectionEnd = end;
        return true;
    }

    private static int AddRows(VStack root, HelpModel model, int start, TerminalVisualOutputOptions options)
    {
        var firstRow = model.Lines[start].Row!;
        var useTable = firstRow.Kind switch
        {
            HelpRowKind.Option => options.UseTableForOptions,
            HelpRowKind.Argument => options.UseTableForArguments,
            HelpRowKind.Command => options.UseTableForCommands,
            _ => options.UseTableForOptions,
        };

        var end = start;
        while (end + 1 < model.Lines.Count && model.Lines[end + 1].Kind == HelpLineKind.Row && model.Lines[end + 1].Row?.Kind == firstRow.Kind)
        {
            end++;
        }

        if (useTable)
        {
            var table = new Table();
            table.SetStyle(options.TableStyleOverride ?? DefaultTableStyle);

            for (var index = start; index <= end; index++)
            {
                var row = model.Lines[index].Row!;
                table.AddRow(
                    new Markup(MarkupStyleHelper.ApplyStyle(GetPrototypeStyle(options, row.Kind), row.Prototype)),
                    new Markup(MarkupStyleHelper.ApplyStyle(options.DescriptionStyle, row.Description)));
            }

            root.Add(table);
            return end;
        }

        for (var index = start; index <= end; index++)
        {
            var row = model.Lines[index].Row!;
            var markup = $"{MarkupStyleHelper.ApplyStyle(GetPrototypeStyle(options, row.Kind), row.Prototype)} {MarkupStyleHelper.Escape(row.Description)}";
            root.Add(new Markup(markup));
        }

        return end;
    }

    private static string GetPrototypeStyle(TerminalVisualOutputOptions options, HelpRowKind rowKind)
    {
        return rowKind switch
        {
            HelpRowKind.Option => options.OptionPrototypeStyle,
            HelpRowKind.Argument => options.ArgumentPrototypeStyle,
            HelpRowKind.Command => options.CommandNameStyle,
            _ => options.OptionPrototypeStyle,
        };
    }

    private static readonly TableStyle DefaultTableStyle = TableStyle.Minimal with
    {
        ShowHeaderSeparator = false,
        CellPadding = new Thickness(1, 0, 1, 0),
    };

    private static bool IsGroupHeader(string? text)
    {
        return !string.IsNullOrWhiteSpace(text) && text.TrimEnd().EndsWith(":", StringComparison.Ordinal);
    }

    private static string GetGroupTitle(string text)
    {
        var trimmed = text.TrimEnd();
        if (trimmed.EndsWith(":", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^1].TrimEnd();
        }
        return trimmed;
    }
}
