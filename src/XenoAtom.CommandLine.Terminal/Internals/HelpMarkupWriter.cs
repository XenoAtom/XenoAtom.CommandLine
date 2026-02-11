using System;
using System.Collections.Generic;
using XenoAtom.CommandLine.Terminal.Internals;
using XenoAtom.Terminal;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal static class HelpMarkupWriter
{
    public static void Write(Command command, CommandRunConfig runConfig, TerminalMarkupOutputOptions options, HelpModel model)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(runConfig);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(model);

        var width = ResolveWidth(runConfig, options);
        var optionWidth = Math.Min(runConfig.OptionWidth, Math.Max(8, width - 8));
        var descriptionFirstWidth = Math.Max(2, width - optionWidth);
        var descriptionRemainingWidth = Math.Max(2, width - optionWidth - 2);
        var wrappedIndent = new string(' ', optionWidth + 2);

        MarkupAtomicWriter.Write(writer =>
        {
            foreach (var line in model.Lines)
            {
                switch (line.Kind)
                {
                    case HelpLineKind.Blank:
                        writer.WriteMarkupLine(string.Empty);
                        break;

                    case HelpLineKind.Usage:
                        WriteWrappedText(writer, line.Text ?? string.Empty, options.UsageStyle, string.Empty, width, width);
                        break;

                    case HelpLineKind.Text:
                        WriteWrappedText(
                            writer,
                            line.Text ?? string.Empty,
                            line.IsSectionHeader ? options.SectionHeaderStyle : options.DescriptionStyle,
                            string.Empty,
                            width,
                            width);
                        break;

                    case HelpLineKind.FooterHint:
                        WriteWrappedText(writer, line.Text ?? string.Empty, options.HintStyle, string.Empty, width, width);
                        break;

                    case HelpLineKind.Row when line.Row is not null:
                        WriteRow(writer, line.Row, options, optionWidth, descriptionFirstWidth, descriptionRemainingWidth, wrappedIndent);
                        break;
                }
            }
        });
    }

    private static void WriteRow(
        MarkupAtomicWriter.Writer writer,
        HelpRow row,
        TerminalMarkupOutputOptions options,
        int optionWidth,
        int descriptionFirstWidth,
        int descriptionRemainingWidth,
        string wrappedIndent)
    {
        var prototypeStyle = row.Kind switch
        {
            HelpRowKind.Option => options.OptionPrototypeStyle,
            HelpRowKind.Argument => options.ArgumentPrototypeStyle,
            HelpRowKind.Command => options.CommandNameStyle,
            _ => options.OptionPrototypeStyle,
        };

        var prototypeMarkup = MarkupStyleHelper.ApplyStyle(prototypeStyle, row.Prototype);
        var description = row.Description ?? string.Empty;
        var descriptionLines = new List<string>(TextWrapper.WrapLines(description, descriptionFirstWidth, descriptionRemainingWidth));

        if (descriptionLines.Count == 0)
        {
            writer.WriteMarkupLine(prototypeMarkup);
            return;
        }

        if (row.Prototype.Length < optionWidth)
        {
            var firstDescriptionMarkup = MarkupStyleHelper.ApplyStyle(options.DescriptionStyle, descriptionLines[0]);
            writer.WriteMarkupLine($"{prototypeMarkup}{new string(' ', optionWidth - row.Prototype.Length)}{firstDescriptionMarkup}");
        }
        else
        {
            writer.WriteMarkupLine(prototypeMarkup);
            var firstDescriptionMarkup = MarkupStyleHelper.ApplyStyle(options.DescriptionStyle, descriptionLines[0]);
            writer.WriteMarkupLine($"{new string(' ', optionWidth)}{firstDescriptionMarkup}");
        }

        for (var index = 1; index < descriptionLines.Count; index++)
        {
            var descriptionMarkup = MarkupStyleHelper.ApplyStyle(options.DescriptionStyle, descriptionLines[index]);
            writer.WriteMarkupLine($"{wrappedIndent}{descriptionMarkup}");
        }
    }

    private static void WriteWrappedText(MarkupAtomicWriter.Writer writer, string text, string style, string prefix, int firstWidth, int remainingWidth)
    {
        var lines = TextWrapper.WrapLines(text, firstWidth, remainingWidth);
        var usePrefix = false;
        foreach (var line in lines)
        {
            var lineMarkup = MarkupStyleHelper.ApplyStyle(style, line);
            if (usePrefix)
            {
                writer.WriteMarkupLine($"{prefix}{lineMarkup}");
            }
            else
            {
                writer.WriteMarkupLine(lineMarkup);
            }

            usePrefix = true;
        }
    }

    private static int ResolveWidth(CommandRunConfig runConfig, TerminalMarkupOutputOptions options)
    {
        if (options.WidthOverride is int widthOverride && widthOverride > 0)
        {
            return widthOverride;
        }

        if (options.UseTerminalWindowWidth)
        {
            try
            {
                var windowWidth = XenoAtom.Terminal.Terminal.WindowWidth;
                if (windowWidth > 0)
                {
                    return windowWidth;
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        return Math.Max(20, runConfig.Width);
    }
}
