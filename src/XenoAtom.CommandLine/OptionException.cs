// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace XenoAtom.CommandLine;

/// <summary>
/// Represents an exception that is thrown when an error occurs while parsing options.
/// </summary>
public class OptionException : CommandException
{
    /// <summary>
    /// Creates a new instance of <see cref="OptionException"/>.
    /// </summary>
    /// <param name="message">The message of this exception.</param>
    /// <param name="optionName">The associated option.</param>
    public OptionException(string message, string optionName)
        : base(message)
    {
        OptionName = optionName;
    }

    /// <summary>
    /// Creates a new instance of <see cref="OptionException"/>.
    /// </summary>
    /// <param name="message">The message of this exception.</param>
    /// <param name="optionName">The associated option.</param>
    /// <param name="innerException">The associated inner exception.</param>
    public OptionException(string message, string optionName, Exception innerException)
        : base(message, innerException)
    {
        OptionName = optionName;
    }

    /// <summary>
    /// Gets the associated name of the option for this exception.
    /// </summary>
    public string OptionName { get; }
}