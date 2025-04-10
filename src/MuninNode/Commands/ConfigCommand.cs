using System.Buffers;
using System.Text;
using MuninNode.Plugins;

namespace MuninNode.Commands;

public class ConfigCommand(IPluginProvider pluginProvider) : ICommand
{
    public ReadOnlySpan<byte> Name => "config"u8;

    private static Encoding Encoding => Encoding.Default;

    private static readonly string[] UnknownService =
    [
    ];

    public Task<HanldeResult> ProcessAsync(ReadOnlySequence<byte> arguments, CancellationToken cancellationToken)
    {
        var plugin = pluginProvider.Plugins.FirstOrDefault(
            plugin => string.Equals(Encoding.GetString(arguments), plugin.Name, StringComparison.Ordinal)
        );
        if (plugin == null)
        {
            var result = new HanldeResult
            {
                Lines = new List<string>
                {
                    "# Unknown service",
                    "."
                },
                Status = Status.Continue
            };
            return Task.FromResult(result);
        }

        var responseLines = new List<string>(capacity: 20);

        responseLines.AddRange(
            plugin.GraphAttributes.EnumerateAttributes()
        );

        // The fields referenced by {fieldname}.negative must be defined ahread of others,
        // and thus lists the negative field settings first.
        // Otherwise, the following error occurs when generating the graph.
        // "[RRD ERROR] Unable to graph /var/cache/munin/www/XXX.png : undefined v name XXXXXXXXXXXXXX"
        var orderedFields = plugin.Fields.OrderBy(f => IsNegativeField(f, plugin.Fields) ? 0 : 1);

        foreach (var field in orderedFields)
        {
            bool? graph = null;

            responseLines.Add($"{field.Name}.label {field.Label}");

            var draw = TranslateFieldDrawAttribute(field.GraphStyle);

            if (draw is not null)
            {
                responseLines.Add($"{field.Name}.draw {draw}");
            }

            if (field.WarningRange.HasValue)
            {
                AddFieldValueRange("warning", field.WarningRange);
            }

            if (field.CriticalRange.HasValue)
            {
                AddFieldValueRange("critical", field.CriticalRange);
            }

            if (!string.IsNullOrEmpty(field.NegativeFieldName))
            {
                var negativeField = plugin.Fields.FirstOrDefault(
                    f => string.Equals(field.NegativeFieldName, f.Name, StringComparison.Ordinal)
                );

                if (negativeField is not null)
                {
                    responseLines.Add($"{field.Name}.negative {negativeField.Name}");
                }
            }

// this field is defined as the negative field of other field, so should not be graphed
            if (IsNegativeField(field, plugin.Fields))
            {
                graph = false;
            }

            if (graph is bool drawGraph)
            {
                responseLines.Add($"{field.Name}.graph {(drawGraph ? "yes" : "no")}");
            }
            
            void AddFieldValueRange(string attr, ValueRange range)
            {
                if (range is { Min: not null, Max: not null })
                {
                    responseLines.Add($"{field.Name}.{attr} {range.Min.Value}:{range.Max.Value}");
                }
                else if (range.Min.HasValue)
                {
                    responseLines.Add($"{field.Name}.{attr} {range.Min.Value}:");
                }
                else if (range.Max.HasValue)
                {
                    responseLines.Add($"{field.Name}.{attr} :{range.Max.Value}");
                }
            }
        }

        responseLines.Add(".");


        static bool IsNegativeField(FieldBase field, IReadOnlyCollection<FieldBase> fields)
            => fields.Any(
                f => string.Equals(field.Name, f.NegativeFieldName, StringComparison.Ordinal)
            );

        var result2 = new HanldeResult
        {
            Lines = responseLines,
            Status = Status.Continue
        };
        return Task.FromResult(result2);
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
            _ => throw new InvalidOperationException($"undefined draw attribute value: ({(int)style} {style})")
        };
}