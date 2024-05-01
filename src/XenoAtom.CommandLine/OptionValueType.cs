// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.CommandLine;

/// <summary>
/// Specifies the option value type None, Optional or Required.
/// </summary>
public enum OptionValueType
{
    /// <summary>
    /// No value is required.
    /// </summary>
    None,

    /// <summary>
    /// The option value is optional.
    /// </summary>
    Optional,

    /// <summary>
    /// The option value is required.
    /// </summary>
    Required,
}