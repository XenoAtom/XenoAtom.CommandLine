// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Rendering;
using TerminalHost = XenoAtom.Terminal.Terminal;

namespace XenoAtom.CommandLine.Terminal;

internal sealed class TerminalVisualNode : CommandNode, ICommandNodeDescriptor, IHelpPreformattedContent
{
    private readonly object _cacheLock = new();
    private readonly Dictionary<int, string> _cachedTextByWidth = new();

    public TerminalVisualNode(Visual visual, string? fallbackText = null, Func<bool>? active = null)
        : base(active)
    {
        Visual = visual ?? throw new ArgumentNullException(nameof(visual));
        Description = fallbackText;
    }

    public Visual Visual { get; }

    public string? Description { get; }

    public void WriteTo(TextWriter writer, CommandRunConfig runConfig)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(runConfig);

        if (Visual.Parent is not null)
        {
            if (!string.IsNullOrWhiteSpace(Description))
            {
                writer.WriteLine(Description);
            }

            return;
        }

        var width = Math.Max(20, runConfig.Width);
        var renderedText = GetOrRenderText(width);
        if (renderedText.Length == 0)
        {
            if (!string.IsNullOrWhiteSpace(Description))
            {
                writer.WriteLine(Description);
            }

            return;
        }

        writer.Write(renderedText);
        if (!renderedText.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            writer.WriteLine();
        }
    }

    private string GetOrRenderText(int width)
    {
        lock (_cacheLock)
        {
            if (_cachedTextByWidth.TryGetValue(width, out var cached))
            {
                return cached;
            }
        }

        var rendered = RenderToText(width);

        lock (_cacheLock)
        {
            _cachedTextByWidth[width] = rendered;
        }

        return rendered;
    }

    private string RenderToText(int width)
    {
        if (TerminalHost.IsInitialized)
        {
            return RenderFromSnapshot(width);
        }

        var capabilities = new TerminalCapabilities
        {
            AnsiEnabled = false,
            ColorLevel = TerminalColorLevel.None,
            IsOutputRedirected = true,
            IsInputRedirected = true,
            TerminalName = "InMemory",
        };

        var backend = new InMemoryTerminalBackend(new TerminalSize(width, 40), capabilities);
        using var session = TerminalHost.Open(backend, new TerminalOptions { ImplicitStartInput = false }, force: true);
        session.Instance.Write(Visual);
        return NormalizeBlockText(backend.GetOutText());
    }

    private string RenderFromSnapshot(int width)
    {
        var buffer = VisualSnapshotRenderer.Render(Visual, width);
        var lines = buffer.ToMarkupLines();
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < lines.Count; index++)
        {
            builder.AppendLine(StripMarkup(lines[index]).TrimEnd());
        }

        return builder.ToString();
    }

    private static string NormalizeBlockText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        var hasTrailingNewline = normalized.EndsWith('\n');
        var lines = normalized.Split('\n');
        var count = hasTrailingNewline ? lines.Length - 1 : lines.Length;
        var builder = new StringBuilder(normalized.Length);

        for (var index = 0; index < count; index++)
        {
            builder.Append(lines[index].TrimEnd());
            if (index < count - 1 || hasTrailingNewline)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string StripMarkup(string markup)
    {
        if (string.IsNullOrEmpty(markup))
        {
            return string.Empty;
        }

        var result = new StringBuilder(markup.Length);
        var span = markup.AsSpan();
        var index = 0;
        while (index < span.Length)
        {
            if (span[index] == '[')
            {
                if (index + 1 < span.Length && span[index + 1] == '[')
                {
                    result.Append('[');
                    index += 2;
                    continue;
                }

                var endIndex = span[index..].IndexOf(']');
                if (endIndex >= 0)
                {
                    index += endIndex + 1;
                    continue;
                }
            }
            else if (span[index] == ']' && index + 1 < span.Length && span[index + 1] == ']')
            {
                result.Append(']');
                index += 2;
                continue;
            }

            result.Append(span[index]);
            index++;
        }

        return result.ToString();
    }
}
