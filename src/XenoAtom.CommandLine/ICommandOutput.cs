// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;

namespace XenoAtom.CommandLine;

/// <summary>
/// Identifies where a diagnostic value originated from.
/// </summary>
public enum CommandDiagnosticSource
{
    /// <summary>
    /// The value originated from the command line token stream passed to <c>RunAsync</c>.
    /// </summary>
    CommandLine,

    /// <summary>
    /// The value originated from a response file (for example <c>@args.txt</c>).
    /// </summary>
    ResponseFile,

    /// <summary>
    /// The value originated from an environment variable fallback.
    /// </summary>
    EnvironmentVariable,

    /// <summary>
    /// Other or unknown origin.
    /// </summary>
    Other,
}

/// <summary>
/// Identifies a span within a token in a command line token stream.
/// </summary>
/// <param name="TokenIndex">The 0-based token index within the invocation token list.</param>
/// <param name="Start">The 0-based character start within the token string.</param>
/// <param name="Length">The length within the token string.</param>
public readonly record struct CommandTokenSpan(int TokenIndex, int Start, int Length);

/// <summary>
/// Provides optional structured diagnostic context for rich error rendering.
/// </summary>
/// <remarks>
/// This data is intended for presentation only (for example re-printing the invocation and
/// underlining a token). It must not include secret values.
/// </remarks>
/// <param name="Source">The origin of the diagnostic value.</param>
/// <param name="SourceName">An optional source name (for example environment variable name or response-file name).</param>
/// <param name="Node">The associated command node, when available.</param>
/// <param name="Tokens">The original invocation tokens, when available.</param>
/// <param name="TokenSpan">The location of the relevant token span, when available.</param>
public readonly record struct CommandDiagnostic(
    CommandDiagnosticSource Source,
    string? SourceName,
    CommandNode? Node,
    IReadOnlyList<string>? Tokens,
    CommandTokenSpan? TokenSpan);

/// <summary>
/// Identifies how unknown tokens should be described.
/// </summary>
public enum UnknownTokenKind
{
    /// <summary>
    /// The token could be either a sub-command name or an option-like token, depending on parsing mode.
    /// </summary>
    UnknownCommandOrOption,

    /// <summary>
    /// The token is treated as an unknown option-like token.
    /// </summary>
    UnknownOption,
}

/// <summary>
/// Describes an unknown token along with suggestions and optional diagnostics.
/// </summary>
/// <param name="Token">The unrecognized token.</param>
/// <param name="Suggestions">Suggested corrections, if any.</param>
/// <param name="InactiveMatchMessage">A note if the token matches an inactive command/option, or null.</param>
/// <param name="TokenSpan">The optional location of this token in the invocation token stream.</param>
public readonly record struct UnknownTokenInfo(
    string Token,
    IReadOnlyList<string> Suggestions,
    string? InactiveMatchMessage,
    CommandTokenSpan? TokenSpan = null);

/// <summary>
/// Represents an unknown-token output report with optional invocation tokens.
/// </summary>
/// <param name="Kind">The kind of unknown-token report.</param>
/// <param name="UnknownTokens">One or more unknown-token entries.</param>
/// <param name="InvocationTokens">The invocation tokens used to build diagnostics, when available.</param>
public readonly record struct UnknownTokenReport(
    UnknownTokenKind Kind,
    IReadOnlyList<UnknownTokenInfo> UnknownTokens,
    IReadOnlyList<string>? InvocationTokens = null);

/// <summary>
/// Defines the output handler for all user-visible output produced by the command-line parser:
/// help text, error messages, version display, and license headers.
/// </summary>
public interface ICommandOutput
{
    /// <summary>
    /// Renders the help/usage for the specified command.
    /// </summary>
    /// <param name="command">The command whose help should be displayed.</param>
    /// <param name="runConfig">The run configuration providing output streams and layout hints.</param>
    void WriteHelp(Command command, CommandRunConfig runConfig);

    /// <summary>
    /// Renders a command exception (parse error, validation error, and so on).
    /// </summary>
    /// <param name="command">The command that was being parsed when the error occurred.</param>
    /// <param name="runConfig">The run configuration providing output streams.</param>
    /// <param name="exception">The exception describing the error.</param>
    void WriteError(Command command, CommandRunConfig runConfig, CommandException exception);

    /// <summary>
    /// Renders an error report for unknown token(s).
    /// </summary>
    /// <param name="command">The command context where the unknown token was encountered.</param>
    /// <param name="runConfig">The run configuration providing output streams.</param>
    /// <param name="report">The unknown-token report to render.</param>
    void WriteUnknownTokens(Command command, CommandRunConfig runConfig, UnknownTokenReport report);

    /// <summary>
    /// Renders the version string.
    /// </summary>
    /// <param name="command">The command that owns the version option.</param>
    /// <param name="runConfig">The run configuration providing output streams.</param>
    /// <param name="version">The version string to display.</param>
    void WriteVersion(Command command, CommandRunConfig runConfig, string version);

    /// <summary>
    /// Renders the license header.
    /// </summary>
    /// <param name="command">The command app.</param>
    /// <param name="runConfig">The run configuration providing output streams.</param>
    /// <param name="licenseText">The license text to display.</param>
    void WriteLicenseHeader(Command command, CommandRunConfig runConfig, string licenseText);
}

