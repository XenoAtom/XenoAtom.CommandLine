using System;
using XenoAtom.Ansi;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal static class MarkupStyleHelper
{
    public static string Escape(string? text) => AnsiMarkup.Escape(text ?? string.Empty);

    public static string ApplyStyle(string style, string text)
    {
        var escaped = Escape(text);
        if (string.IsNullOrWhiteSpace(style) || style == "[/]")
        {
            return escaped;
        }

        return $"{style}{escaped}[/]";
    }

    public static string ApplyStyles(string firstStyle, string firstText, string secondStyle, string secondText)
    {
        if (string.IsNullOrEmpty(secondText))
        {
            return ApplyStyle(firstStyle, firstText);
        }

        var first = ApplyStyle(firstStyle, firstText);
        var second = ApplyStyle(secondStyle, secondText);
        if (first.Length == 0)
        {
            return second;
        }

        if (second.Length == 0)
        {
            return first;
        }

        return $"{first} {second}";
    }
}
