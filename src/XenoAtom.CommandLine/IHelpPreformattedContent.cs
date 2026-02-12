// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.IO;

namespace XenoAtom.CommandLine;

/// <summary>
/// Provides preformatted help content that should be written verbatim.
/// </summary>
/// <remarks>
/// When a node implements both <see cref="IHelpPreformattedContent"/> and <see cref="ICommandNodeDescriptor"/>,
/// text outputs should prefer <see cref="WriteTo"/> and use descriptor text as fallback only when preformatted
/// rendering is not supported.
/// </remarks>
public interface IHelpPreformattedContent
{
    /// <summary>
    /// Writes preformatted help content to the specified <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The writer to output content to.</param>
    /// <param name="runConfig">The current command run configuration.</param>
    void WriteTo(TextWriter writer, CommandRunConfig runConfig);
}
