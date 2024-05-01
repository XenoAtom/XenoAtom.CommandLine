// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
            StringBuilder arg = new StringBuilder();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                int t = line.Length;

                for (int i = 0; i < t; i++)
                {
                    char c = line[i];

                    if (c == '"' || c == '\'')
                    {
                        char end = c;

                        for (i++; i < t; i++)
                        {
                            c = line[i];

                            if (c == end)
                                break;
                            arg.Append(c);
                        }
                    }
                    else if (c == ' ')
                    {
                        if (arg.Length > 0)
                        {
                            yield return arg.ToString();
                            arg.Length = 0;
                        }
                    }
                    else
                        arg.Append(c);
                }
                if (arg.Length > 0)
                {
                    yield return arg.ToString();
                    arg.Length = 0;
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