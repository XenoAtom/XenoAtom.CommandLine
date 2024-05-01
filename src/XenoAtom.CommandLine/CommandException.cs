// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// Represents an exception that is thrown when an error occurs when executing a command.
/// </summary>
public class CommandException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="OptionException"/>.
    /// </summary>
    /// <param name="message">The message of this exception.</param>
    public CommandException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="OptionException"/>.
    /// </summary>
    /// <param name="message">The message of this exception.</param>
    /// <param name="innerException">The associated inner exception.</param>
    public CommandException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}