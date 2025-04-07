// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

#pragma warning disable IDE0040
partial class PluginFactory
{
#pragma warning restore IDE0040
    /// <summary>Create a plugin with one field which fetches the value from a delegate.</summary>
    public static IPlugin CreatePlugin(
        string name,
        string fieldLabel,
        Func<double?> fetchFieldValue,
        GraphAttributes graphAttributes
    )
        => CreatePlugin(
            name: name,
            fieldLabel: fieldLabel,
            fieldGraphStyle: GraphStyle.Default,
            fetchFieldValue: fetchFieldValue,
            graphAttributes: graphAttributes
        );

    /// <summary>Create a plugin with one field which fetches the value from a delegate.</summary>
    public static IPlugin CreatePlugin(
        string name,
        string fieldLabel,
        GraphStyle fieldGraphStyle,
        Func<double?> fetchFieldValue,
        GraphAttributes graphAttributes
    )
        => CreatePlugin(
            name: name,
            graphAttributes: graphAttributes,
            field: new ValueFromFuncField(
                label: fieldLabel,
                name: null,
                graphStyle: fieldGraphStyle,
                normalRangeForWarning: FieldNormalValueRange.None,
                normalRangeForCritical: FieldNormalValueRange.None,
                negativeFieldName: null,
                fetchValue: fetchFieldValue
            )
        );

    /// <summary>Create a plugin which has one field.</summary>
    public static IPlugin CreatePlugin(
        string name,
        GraphAttributes graphAttributes,
        FieldBase field
    )
        => CreatePlugin(
            name: name,
            graphAttributes: graphAttributes,
            fields: new[] { field ?? throw new ArgumentNullException(nameof(field)) }
        );

    /// <summary>Create a plugin which has multiple fields.</summary>
    public static IPlugin CreatePlugin(
        string name,
        GraphAttributes graphAttributes,
        IReadOnlyCollection<FieldBase> fields
    )
        => new Plugin(
            name: name,
            graphAttributes: graphAttributes,
            fields: fields ?? throw new ArgumentNullException(nameof(fields))
        );

    /// <summary>Create a plugin which has multiple fields.</summary>
    public static IPlugin CreatePlugin(
        string name,
        GraphAttributes graphAttributes,
        IReadOnlyCollection<IField> fields
    )
        => new Plugin(
            name: name,
            graphAttributes: graphAttributes,
            fields: fields ?? throw new ArgumentNullException(nameof(fields))
        );
}