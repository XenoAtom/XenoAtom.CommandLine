// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XenoAtom.CommandLine;

/// <summary>
/// Internal class used for displaying help.
/// </summary>
internal static class StringCoda
{
    public static IEnumerable<string> WrappedLines(string self, params int[] widths)
    {
        IEnumerable<int> w = widths;
        return WrappedLines(self, w);
    }

    public static IEnumerable<string> WrappedLines(string self, IEnumerable<int> widths)
    {
        if (widths == null)
            throw new ArgumentNullException("widths");
        return CreateWrappedLinesIterator(self, widths);
    }

    private static IEnumerable<string> CreateWrappedLinesIterator(string self, IEnumerable<int> widths)
    {
        if (string.IsNullOrEmpty(self))
        {
            yield return string.Empty;
            yield break;
        }
        using (IEnumerator<int> ewidths = widths.GetEnumerator())
        {
            bool? hw = null;
            int width = GetNextWidth(ewidths, int.MaxValue, ref hw);
            int start = 0, end;
            do
            {
                end = GetLineEnd(start, width, self);
                // endCorrection is 1 if the line end is '\n', and might be 2 if the line end is '\r\n'.
                int endCorrection = 1;
                if (end >= 2 && self.Substring(end - 2, 2).Equals("\r\n"))
                    endCorrection = 2;
                char c = self[end - endCorrection];
                if (char.IsWhiteSpace(c))
                    end -= endCorrection;
                bool needContinuation = end != self.Length && !IsEolChar(c);
                string continuation = "";
                if (needContinuation)
                {
                    --end;
                    continuation = "-";
                }
                string line = self.Substring(start, end - start) + continuation;
                yield return line;
                start = end;
                if (char.IsWhiteSpace(c))
                    start += endCorrection;
                width = GetNextWidth(ewidths, width, ref hw);
            } while (start < self.Length);
        }
    }

    private static int GetNextWidth(IEnumerator<int> ewidths, int curWidth, ref bool? eValid)
    {
        if (!eValid.HasValue || (eValid.HasValue && eValid.Value))
        {
            curWidth = (eValid = ewidths.MoveNext()).Value ? ewidths.Current : curWidth;
            // '.' is any character, - is for a continuation
            const string minWidth = ".-";
            if (curWidth < minWidth.Length)
                throw new ArgumentOutOfRangeException("widths",
                    string.Format("Element must be >= {0}, was {1}.", minWidth.Length, curWidth));
            return curWidth;
        }
        // no more elements, use the last element.
        return curWidth;
    }

    private static bool IsEolChar(char c)
    {
        return !char.IsLetterOrDigit(c);
    }

    private static int GetLineEnd(int start, int length, string description)
    {
        int end = System.Math.Min(start + length, description.Length);
        int sep = -1;
        for (int i = start; i < end; ++i)
        {
            if (i + 2 <= description.Length && description.Substring(i, 2).Equals("\r\n"))
                return i + 2;
            if (description[i] == '\n')
                return i + 1;
            if (IsEolChar(description[i]))
                sep = i + 1;
        }
        if (sep == -1 || end == description.Length)
            return end;
        return sep;
    }
}