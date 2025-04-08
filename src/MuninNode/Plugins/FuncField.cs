namespace MuninNode.Plugins;

public sealed class FuncField : FieldBase
{
    private readonly Func<double?> FetchValue;

    public FuncField(
        string label,
        string? name,
        GraphStyle graphStyle,
        ValueRange normalRangeForWarning,
        ValueRange normalRangeForCritical,
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