// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

#pragma warning disable IDE0040
partial class PluginFactory
{
#pragma warning restore IDE0040
    public static IField CreateField(
        string label,
        Func<double?> fetchValue
    )
        => new ValueFromFuncField(
            label: label,
            name: null,
            graphStyle: GraphStyle.Default,
            normalRangeForWarning: FieldNormalValueRange.None,
            normalRangeForCritical: FieldNormalValueRange.None,
            negativeFieldName: null,
            fetchValue: fetchValue
        );

    public static IField CreateField(
        string label,
        GraphStyle graphStyle,
        Func<double?> fetchValue
    )
        => new ValueFromFuncField(
            label: label,
            name: null,
            graphStyle: graphStyle,
            normalRangeForWarning: FieldNormalValueRange.None,
            normalRangeForCritical: FieldNormalValueRange.None,
            negativeFieldName: null,
            fetchValue: fetchValue
        );

    public static IField CreateField(
        string label,
        GraphStyle graphStyle,
        FieldNormalValueRange normalRangeForWarning,
        FieldNormalValueRange normalRangeForCritical,
        Func<double?> fetchValue
    )
        => new ValueFromFuncField(
            label: label,
            name: null,
            graphStyle: graphStyle,
            normalRangeForWarning: normalRangeForWarning,
            normalRangeForCritical: normalRangeForCritical,
            negativeFieldName: null,
            fetchValue: fetchValue
        );

    public static IField CreateField(
        string name,
        string label,
        GraphStyle graphStyle,
        FieldNormalValueRange normalRangeForWarning,
        FieldNormalValueRange normalRangeForCritical,
        Func<double?> fetchValue
    )
        => new ValueFromFuncField(
            label: label,
            name: name,
            graphStyle: graphStyle,
            normalRangeForWarning: normalRangeForWarning,
            normalRangeForCritical: normalRangeForCritical,
            negativeFieldName: null,
            fetchValue: fetchValue
        );

    public static IField CreateField(
        string name,
        string label,
        GraphStyle graphStyle,
        FieldNormalValueRange normalRangeForWarning,
        FieldNormalValueRange normalRangeForCritical,
        string? negativeFieldName,
        Func<double?> fetchValue
    )
        => new ValueFromFuncField(
            label: label,
            name: name,
            graphStyle: graphStyle,
            normalRangeForWarning: normalRangeForWarning,
            normalRangeForCritical: normalRangeForCritical,
            negativeFieldName: negativeFieldName,
            fetchValue: fetchValue
        );

    private sealed class ValueFromFuncField : FieldBase
    {
        private readonly Func<double?> FetchValue;

        public ValueFromFuncField(
            string label,
            string? name,
            GraphStyle graphStyle,
            FieldNormalValueRange normalRangeForWarning,
            FieldNormalValueRange normalRangeForCritical,
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