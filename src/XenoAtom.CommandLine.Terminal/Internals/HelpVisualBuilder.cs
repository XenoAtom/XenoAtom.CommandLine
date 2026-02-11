using System;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal static class HelpVisualBuilder
{
    public static Visual Build(HelpModel model, TerminalHelpVisualOptions options)
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
                    root.Add(new Markup(MarkupStyleHelper.ApplyStyle(options.UsageMarkupStyle, line.Text ?? string.Empty)));
                    continue;

                case HelpLineKind.Text:
                    if (string.IsNullOrEmpty(line.Text))
                    {
                        root.Add(new TextBlock(string.Empty));
                    }
                    else
                    {
                        var style = line.IsSectionHeader ? "[bold]" : options.DimMarkupStyle;
                        root.Add(new Markup(MarkupStyleHelper.ApplyStyle(style, line.Text)));
                    }
                    continue;

                case HelpLineKind.FooterHint:
                    root.Add(new Markup(MarkupStyleHelper.ApplyStyle(options.DimMarkupStyle, line.Text ?? string.Empty)));
                    continue;

                case HelpLineKind.Row when line.Row is not null:
                    index = AddRows(root, model, index, options);
                    continue;
            }
        }

        return root;
    }

    private static int AddRows(VStack root, HelpModel model, int start, TerminalHelpVisualOptions options)
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
                    new Markup(MarkupStyleHelper.ApplyStyle("[/]", row.Description)));
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

    private static string GetPrototypeStyle(TerminalHelpVisualOptions options, HelpRowKind rowKind)
    {
        return rowKind switch
        {
            HelpRowKind.Option => options.OptionMarkupStyle,
            HelpRowKind.Argument => options.ArgumentMarkupStyle,
            HelpRowKind.Command => options.CommandNameMarkupStyle,
            _ => options.OptionMarkupStyle,
        };
    }

    private static readonly TableStyle DefaultTableStyle = TableStyle.Minimal with
    {
        ShowHeaderSeparator = false,
        CellPadding = new Thickness(1, 0, 1, 0),
    };
}
