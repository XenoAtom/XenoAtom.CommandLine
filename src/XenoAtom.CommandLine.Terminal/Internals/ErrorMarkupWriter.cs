// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XenoAtom.CommandLine.Terminal.Internals;

internal static class ErrorMarkupWriter
{
    public static void WriteError(Command command, TerminalMarkupOutputOptions options, CommandException exception)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        MarkupAtomicWriter.Write(writer =>
        {
            var sourceContext = GetSourceContext(exception.Diagnostic);
            var headerText = sourceContext.Length == 0 ? "Error:" : $"Error {sourceContext}:";
            var messageMarkup = $"{MarkupStyleHelper.ApplyStyle(options.ErrorStyle, headerText)} {MarkupStyleHelper.Escape(exception.Message)}";
            writer.WriteMarkupLine(messageMarkup);

            if (options.ShowDiagnosticUnderline)
            {
                WriteDiagnosticUnderline(writer, command, options, exception.Diagnostic);
            }

            writer.WriteMarkupLine(MarkupStyleHelper.ApplyStyle(options.HintStyle, CommandOutputHelper.GetHelpHint(command)));
        });
    }

    public static void WriteUnknownTokens(
        Command command,
        TerminalMarkupOutputOptions options,
        UnknownTokenKind kind,
        IReadOnlyList<UnknownTokenInfo> unknownTokens)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(unknownTokens);

        var messagePrefix = kind == UnknownTokenKind.UnknownCommandOrOption ? "Unknown command or option" : "Unknown option";
        var invocationTokens = options.InvocationTokensProvider?.Invoke();

        MarkupAtomicWriter.Write(writer =>
        {
            foreach (var unknownToken in unknownTokens)
            {
                writer.WriteMarkupLine(
                    $"{MarkupStyleHelper.ApplyStyle(options.ErrorStyle, "Error:")} {messagePrefix}: {MarkupStyleHelper.Escape(unknownToken.Token)}");

                if (options.ShowDiagnosticUnderline && invocationTokens is { Count: > 0 } && unknownToken.TokenSpan is { } tokenSpan)
                {
                    var invocation = CommandOutputHelper.RenderInvocation(command, invocationTokens);
                    writer.WriteMarkupLine(MarkupStyleHelper.ApplyStyle(options.HintStyle, invocation.Text));

                    var underline = CommandOutputHelper.RenderUnderline(invocation, tokenSpan);
                    if (!string.IsNullOrWhiteSpace(underline))
                    {
                        writer.WriteMarkupLine(MarkupStyleHelper.ApplyStyle(options.ErrorStyle, underline));
                    }
                }

                if (!string.IsNullOrWhiteSpace(unknownToken.InactiveMatchMessage))
                {
                    writer.WriteMarkupLine(MarkupStyleHelper.ApplyStyle(options.HintStyle, unknownToken.InactiveMatchMessage));
                }

                if (unknownToken.Suggestions.Count > 0)
                {
                    writer.WriteMarkupLine(
                        MarkupStyleHelper.ApplyStyle(options.HintStyle, $"Did you mean: {string.Join(", ", unknownToken.Suggestions)}"));
                }
            }

            writer.WriteMarkupLine(MarkupStyleHelper.ApplyStyle(options.HintStyle, CommandOutputHelper.GetHelpHint(command)));
        });
    }

    private static void WriteDiagnosticUnderline(
        MarkupAtomicWriter.Writer writer,
        Command command,
        TerminalMarkupOutputOptions options,
        CommandDiagnostic? diagnostic)
    {
        if (diagnostic is not { Tokens: { Count: > 0 } tokens, TokenSpan: { } tokenSpan })
        {
            return;
        }

        var invocation = CommandOutputHelper.RenderInvocation(command, tokens);
        writer.WriteMarkupLine(MarkupStyleHelper.ApplyStyle(options.HintStyle, invocation.Text));

        var underline = CommandOutputHelper.RenderUnderline(invocation, tokenSpan);
        if (!string.IsNullOrWhiteSpace(underline))
        {
            writer.WriteMarkupLine(MarkupStyleHelper.ApplyStyle(options.ErrorStyle, underline));
        }
    }

    private static string GetSourceContext(CommandDiagnostic? diagnostic)
    {
        if (diagnostic is not { } value || value.Source == CommandDiagnosticSource.CommandLine)
        {
            return string.Empty;
        }

        return value.Source switch
        {
            CommandDiagnosticSource.ResponseFile => value.SourceName is null
                ? "(in response file)"
                : $"(in response file '{value.SourceName}')",
            CommandDiagnosticSource.EnvironmentVariable => value.SourceName is null
                ? "(in environment variable)"
                : $"(in environment variable '{value.SourceName}')",
            CommandDiagnosticSource.Other => value.SourceName is null
                ? "(in additional source)"
                : $"(in source '{value.SourceName}')",
            _ => string.Empty,
        };
    }
}
