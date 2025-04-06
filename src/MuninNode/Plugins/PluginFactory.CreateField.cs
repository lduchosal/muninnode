// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

#pragma warning disable IDE0040
partial class PluginFactory
{
#pragma warning restore IDE0040
    public static IPluginField CreateField(
        string label,
        Func<double?> fetchValue
    )
        => new ValueFromFuncPluginField(
            label: label,
            name: null,
            graphStyle: GraphStyle.Default,
            normalRangeForWarning: PluginFieldNormalValueRange.None,
            normalRangeForCritical: PluginFieldNormalValueRange.None,
            negativeFieldName: null,
            fetchValue: fetchValue
        );

    public static IPluginField CreateField(
        string label,
        GraphStyle graphStyle,
        Func<double?> fetchValue
    )
        => new ValueFromFuncPluginField(
            label: label,
            name: null,
            graphStyle: graphStyle,
            normalRangeForWarning: PluginFieldNormalValueRange.None,
            normalRangeForCritical: PluginFieldNormalValueRange.None,
            negativeFieldName: null,
            fetchValue: fetchValue
        );

    public static IPluginField CreateField(
        string label,
        GraphStyle graphStyle,
        PluginFieldNormalValueRange normalRangeForWarning,
        PluginFieldNormalValueRange normalRangeForCritical,
        Func<double?> fetchValue
    )
        => new ValueFromFuncPluginField(
            label: label,
            name: null,
            graphStyle: graphStyle,
            normalRangeForWarning: normalRangeForWarning,
            normalRangeForCritical: normalRangeForCritical,
            negativeFieldName: null,
            fetchValue: fetchValue
        );

    public static IPluginField CreateField(
        string name,
        string label,
        GraphStyle graphStyle,
        PluginFieldNormalValueRange normalRangeForWarning,
        PluginFieldNormalValueRange normalRangeForCritical,
        Func<double?> fetchValue
    )
        => new ValueFromFuncPluginField(
            label: label,
            name: name,
            graphStyle: graphStyle,
            normalRangeForWarning: normalRangeForWarning,
            normalRangeForCritical: normalRangeForCritical,
            negativeFieldName: null,
            fetchValue: fetchValue
        );

    public static IPluginField CreateField(
        string name,
        string label,
        GraphStyle graphStyle,
        PluginFieldNormalValueRange normalRangeForWarning,
        PluginFieldNormalValueRange normalRangeForCritical,
        string? negativeFieldName,
        Func<double?> fetchValue
    )
        => new ValueFromFuncPluginField(
            label: label,
            name: name,
            graphStyle: graphStyle,
            normalRangeForWarning: normalRangeForWarning,
            normalRangeForCritical: normalRangeForCritical,
            negativeFieldName: negativeFieldName,
            fetchValue: fetchValue
        );

    private sealed class ValueFromFuncPluginField : PluginFieldBase
    {
        private readonly Func<double?> FetchValue;

        public ValueFromFuncPluginField(
            string label,
            string? name,
            GraphStyle graphStyle,
            PluginFieldNormalValueRange normalRangeForWarning,
            PluginFieldNormalValueRange normalRangeForCritical,
            string? negativeFieldName,
            Func<double?> fetchValue
        )
            : base(
                label: label,
                name: name,
                graphStyle: graphStyle,
                normalRangeForWarning: normalRangeForWarning,
                normalRangeForCritical: normalRangeForCritical,
                negativeFieldName: negativeFieldName
            )
        {
            this.FetchValue = fetchValue ?? throw new ArgumentNullException(nameof(fetchValue));
        }

        protected override ValueTask<double?> FetchValueAsync(CancellationToken cancellationToken)
            => new(FetchValue());
    }
}