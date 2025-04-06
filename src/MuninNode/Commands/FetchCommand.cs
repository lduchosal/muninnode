using System.Buffers;
using System.Text;
using MuninNode.Plugins;

namespace MuninNode.Commands;

public class FetchCommand(IPluginProvider pluginProvider) : ICommand, IDefaultCommand
{
    public ReadOnlySpan<byte> Name => "fetch"u8;
    private static Encoding Encoding => Encoding.Default;

    private static readonly string[] ResponseLinesUnknownService =
    [
        "# Unknown service",
        "."
    ];

    public async Task<string[]> ProcessAsync(ReadOnlySequence<byte> arguments, CancellationToken cancellationToken)
    {
        var plugin = pluginProvider.Plugins.FirstOrDefault(
            plugin => string.Equals(Encoding.GetString(arguments), plugin.Name, StringComparison.Ordinal)
        );

        if (plugin == null)
        {
            return ResponseLinesUnknownService;
        }

        var responseLines = new List<string>(capacity: plugin.DataSource.Fields.Count + 1);

        foreach (var field in plugin.DataSource.Fields)
        {
            var valueString = await field.GetFormattedValueStringAsync(
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            responseLines.Add($"{field.Name}.value {valueString}");
        }

        responseLines.Add(".");
        return responseLines.ToArray();
    }
}