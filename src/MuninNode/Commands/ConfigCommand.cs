using System.Buffers;
using System.Text;
using MuninNode.Plugins;

namespace MuninNode.Commands;

public class ConfigCommand(IPluginProvider pluginProvider) : ICommand
{
    private Encoding Encoding => Encoding.Default;
    private static readonly string[] ResponseLinesUnknownService =
    [
        "# Unknown service",
        ".",
    ];
    public Task<string[]> ProcessAsync(ReadOnlySequence<byte> arguments, CancellationToken cancellationToken)
    {

        var plugin = pluginProvider.Plugins.FirstOrDefault(
            plugin => string.Equals(Encoding.GetString(arguments), plugin.Name, StringComparison.Ordinal)
        );
        if (plugin == null)
        {
            return Task.FromResult(ResponseLinesUnknownService);
        }

        var responseLines = new List<string>(capacity: 20);

        responseLines.AddRange(
            plugin.GraphAttributes.EnumerateAttributes()
        );

// The fields referenced by {fieldname}.negative must be defined ahread of others,
// and thus lists the negative field settings first.
// Otherwise, the following error occurs when generating the graph.
// "[RRD ERROR] Unable to graph /var/cache/munin/www/XXX.png : undefined v name XXXXXXXXXXXXXX"
        var orderedFields = plugin.DataSource.Fields.OrderBy(f => IsNegativeField(f, plugin.DataSource.Fields) ? 0 : 1);

        foreach (var field in orderedFields)
        {
            var fieldAttrs = field.Attributes;
            bool? graph = null;

            responseLines.Add($"{field.Name}.label {fieldAttrs.Label}");

            var draw = TranslateFieldDrawAttribute(fieldAttrs.GraphStyle);

            if (draw is not null)
                responseLines.Add($"{field.Name}.draw {draw}");

            if (fieldAttrs.NormalRangeForWarning.HasValue)
                AddFieldValueRange("warning", fieldAttrs.NormalRangeForWarning);

            if (fieldAttrs.NormalRangeForCritical.HasValue)
                AddFieldValueRange("critical", fieldAttrs.NormalRangeForCritical);

            if (!string.IsNullOrEmpty(fieldAttrs.NegativeFieldName))
            {
                var negativeField = plugin.DataSource.Fields.FirstOrDefault(
                    f => string.Equals(fieldAttrs.NegativeFieldName, f.Name, StringComparison.Ordinal)
                );

                if (negativeField is not null)
                    responseLines.Add($"{field.Name}.negative {negativeField.Name}");
            }

// this field is defined as the negative field of other field, so should not be graphed
            if (IsNegativeField(field, plugin.DataSource.Fields))
                graph = false;

            if (graph is bool drawGraph)
                responseLines.Add($"{field.Name}.graph {(drawGraph ? "yes" : "no")}");

            void AddFieldValueRange(string attr, PluginFieldNormalValueRange range)
            {
                if (range.Min.HasValue && range.Max.HasValue)
                    responseLines.Add($"{field.Name}.{attr} {range.Min.Value}:{range.Max.Value}");
                else if (range.Min.HasValue)
                    responseLines.Add($"{field.Name}.{attr} {range.Min.Value}:");
                else if (range.Max.HasValue)
                    responseLines.Add($"{field.Name}.{attr} :{range.Max.Value}");
            }
        }

        responseLines.Add(".");


        static bool IsNegativeField(IPluginField field, IReadOnlyCollection<IPluginField> fields)
            => fields.Any(
                f => string.Equals(field.Name, f.Attributes.NegativeFieldName, StringComparison.Ordinal)
            );
        
        return Task.FromResult(responseLines.ToArray());

    }
    
    private static string? TranslateFieldDrawAttribute(GraphStyle style)
        => style switch
        {
            GraphStyle.Default => null,
            GraphStyle.Area => "AREA",
            GraphStyle.Stack => "STACK",
            GraphStyle.AreaStack => "AREASTACK",
            GraphStyle.Line => "LINE",
            GraphStyle.LineWidth1 => "LINE1",
            GraphStyle.LineWidth2 => "LINE2",
            GraphStyle.LineWidth3 => "LINE3",
            GraphStyle.LineStack => "LINESTACK",
            GraphStyle.LineStackWidth1 => "LINE1STACK",
            GraphStyle.LineStackWidth2 => "LINE2STACK",
            GraphStyle.LineStackWidth3 => "LINE3STACK",
            _ => throw new InvalidOperationException($"undefined draw attribute value: ({(int)style} {style})"),
        };

}