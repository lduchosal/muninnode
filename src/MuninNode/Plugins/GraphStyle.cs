// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.ComponentModel;

namespace MuninNode.Plugins;

/// <summary>
/// Represents the style of how the field should be drawn on the graph.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-draw">Plugin reference - Field name attributes - {fieldname}.draw</seealso>
public enum GraphStyle
{
    [Description("")] Default = default,

    [Description("AREA")] Area = 1,
    [Description("STACK")] Stack = 2,
    [Description("AREASTACK")] AreaStack = 3,

    [Description("LINE")] Line = 100,
    [Description("LINE1")] LineWidth1 = 101,
    [Description("LINE2")] LineWidth2 = 102,
    [Description("LINE3")] LineWidth3 = 103,

    [Description("LINESTACK")] LineStack = 200,
    [Description("LINE1STACK")] LineStackWidth1 = 201,
    [Description("LINE2STACK")] LineStackWidth2 = 202,
    [Description("LINE3STACK")] LineStackWidth3 = 203
}
