// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// Represents an exception that is thrown when an error occurs while parsing command arguments.
/// </summary>
public class CommandArgumentException : CommandException
{
    /// <summary>
    /// Creates a new instance of <see cref="CommandArgumentException"/>.
    /// </summary>
    /// <param name="message">The message of this exception.</param>
    /// <param name="argumentName">The associated argument.</param>
    public CommandArgumentException(string message, string argumentName)
        : base(message)
    {
        ArgumentName = argumentName;
    }

    /// <summary>
    /// Creates a new instance of <see cref="CommandArgumentException"/>.
    /// </summary>
    /// <param name="message">The message of this exception.</param>
    /// <param name="argumentName">The associated argument.</param>
    /// <param name="innerException">The associated inner exception.</param>
    public CommandArgumentException(string message, string argumentName, Exception innerException)
        : base(message, innerException)
    {
        ArgumentName = argumentName;
    }

    /// <summary>
    /// Gets the associated name of the argument for this exception.
    /// </summary>
    public string ArgumentName { get; }
}

