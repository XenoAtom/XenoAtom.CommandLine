// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace XenoAtom.CommandLine;

/// <summary>
/// A provider of arguments for a command.
/// </summary>
public abstract class ArgumentSource : CommandNode, ICommandNodeDescriptor
{
    /// <summary>
    /// Base constructor for <see cref="ArgumentSource"/>.
    /// </summary>
    protected ArgumentSource()
    {
    }

    /// <summary>
    /// Gets the names of this argument source.
    /// </summary>
    public abstract string[] GetNames();

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <summary>
    /// Tries to get the arguments from the specified value.
    /// </summary>
    /// <param name="value">The value to get the argument from.</param>
    /// <param name="arguments">The expanded arguments if this method return true.</param>
    /// <returns><c>true</c> if this instance is processing the value; false otherwise.</returns>
    public abstract bool TryGetArguments(string value, [NotNullWhen(true)] out IEnumerable<string>? arguments);

    /// <summary>
    /// Gets the arguments from the specified "response" file.
    /// </summary>
    /// <param name="file">A file to get arguments from</param>
    /// <returns>The arguments extracted from the file</returns>
    public static IEnumerable<string> GetArgumentsFromFile(string file)
    {
        return GetArguments(File.OpenText(file), true);
    }

    /// <summary>
    /// Gets the arguments from the specified reader.
    /// </summary>
    /// <param name="reader">A reader to read lines from.</param>
    /// <returns>The arguments extracted from the reader</returns>
    public static IEnumerable<string> GetArguments(TextReader reader)
    {
        return GetArguments(reader, false);
    }

    // Cribbed from mcs/driver.cs:LoadArgs(string)
    private static IEnumerable<string> GetArguments(TextReader reader, bool close)
    {
        try
        {
            var arg = new StringBuilder(64);
            var enableBackslashEscapes = !OperatingSystem.IsWindows();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var i = 0;
                while (i < line.Length)
                {
                    // Skip whitespace.
                    while (i < line.Length && char.IsWhiteSpace(line[i]))
                    {
                        i++;
                    }

                    if (i >= line.Length)
                        break;

                    // Comment-only line (allow leading whitespace).
                    if (line[i] == '#')
                        break;

                    arg.Length = 0;
                    char quote = '\0';

                    while (i < line.Length)
                    {
                        var c = line[i];

                        if (quote != '\0')
                        {
                            if (c == quote)
                            {
                                quote = '\0';
                                i++;
                                continue;
                            }

                            if (enableBackslashEscapes && c == '\\' && i + 1 < line.Length)
                            {
                                var next = line[i + 1];
                                if (next == '\\' || next == '"' || next == '\'' || char.IsWhiteSpace(next) || next == '#')
                                {
                                    // Basic escaping inside quotes.
                                    arg.Append(next);
                                    i += 2;
                                    continue;
                                }
                            }

                            arg.Append(c);
                            i++;
                            continue;
                        }

                        if (char.IsWhiteSpace(c))
                        {
                            break;
                        }

                        if (c == '#')
                        {
                            // Treat as comment start only when not in a token (or after completing one).
                            break;
                        }

                        if (c == '"' || c == '\'')
                        {
                            quote = c;
                            i++;
                            continue;
                        }

                        if (enableBackslashEscapes && c == '\\' && i + 1 < line.Length)
                        {
                            var next = line[i + 1];
                            if (next == '\\' || next == '"' || next == '\'' || char.IsWhiteSpace(next) || next == '#')
                            {
                                // Basic escaping outside quotes.
                                arg.Append(next);
                                i += 2;
                                continue;
                            }
                        }

                        arg.Append(c);
                        i++;
                    }

                    if (arg.Length > 0)
                    {
                        yield return arg.ToString();
                    }

                    // Skip trailing token characters (whitespace or comment).
                    while (i < line.Length && !char.IsWhiteSpace(line[i]))
                    {
                        if (line[i] == '#')
                        {
                            i = line.Length;
                            break;
                        }
                        i++;
                    }
                }
            }
        }
        finally
        {
            if (close)
                reader.Dispose();
        }
    }
}
