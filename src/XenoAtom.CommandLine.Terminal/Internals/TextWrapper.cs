// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal static class TextWrapper
{
    public static IEnumerable<string> WrapLines(string text, int firstWidth, int remainingWidth)
    {
        return WrapLines(text, [firstWidth, remainingWidth]);
    }

    public static IEnumerable<string> WrapLines(string text, IEnumerable<int> widths)
    {
        ArgumentNullException.ThrowIfNull(widths);

        if (string.IsNullOrEmpty(text))
        {
            yield return string.Empty;
            yield break;
        }

        using var widthEnumerator = widths.GetEnumerator();
        bool? hasWidth = null;
        var currentWidth = GetNextWidth(widthEnumerator, int.MaxValue, ref hasWidth);
        var start = 0;

        do
        {
            var end = GetLineEnd(start, currentWidth, text);
            var endCorrection = 1;
            if (end >= 2 && text.Substring(end - 2, 2).Equals("\r\n", StringComparison.Ordinal))
            {
                endCorrection = 2;
            }

            var endingCharacter = text[end - endCorrection];
            if (char.IsWhiteSpace(endingCharacter))
            {
                end -= endCorrection;
            }

            var continuation = string.Empty;
            if (end != text.Length && !IsEolCharacter(endingCharacter))
            {
                end--;
                continuation = "-";
            }

            yield return text.Substring(start, end - start) + continuation;

            start = end;
            if (char.IsWhiteSpace(endingCharacter))
            {
                start += endCorrection;
            }

            currentWidth = GetNextWidth(widthEnumerator, currentWidth, ref hasWidth);
        } while (start < text.Length);
    }

    private static int GetNextWidth(IEnumerator<int> widths, int currentWidth, ref bool? hasWidth)
    {
        if (!hasWidth.HasValue || hasWidth.Value)
        {
            hasWidth = widths.MoveNext();
            currentWidth = hasWidth.Value ? widths.Current : currentWidth;
            if (currentWidth < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(widths), $"Each width must be at least 2. Actual: {currentWidth}.");
            }
        }

        return currentWidth;
    }

    private static bool IsEolCharacter(char character) => !char.IsLetterOrDigit(character);

    private static int GetLineEnd(int start, int length, string text)
    {
        var end = Math.Min(start + length, text.Length);
        var separator = -1;
        for (var index = start; index < end; index++)
        {
            if (index + 2 <= text.Length && text.Substring(index, 2).Equals("\r\n", StringComparison.Ordinal))
            {
                return index + 2;
            }

            if (text[index] == '\n')
            {
                return index + 1;
            }

            if (IsEolCharacter(text[index]))
            {
                separator = index + 1;
            }
        }

        if (separator == -1 || end == text.Length)
        {
            return end;
        }

        return separator;
    }
}
