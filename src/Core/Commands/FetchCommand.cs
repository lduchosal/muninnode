using System.Buffers;
using System.Text;
using MuninNode.Plugins;
using MuninNode.Server;

namespace MuninNode.Commands;

public class FetchCommand(IPluginProvider pluginProvider) : ICommand, IDefaultCommand
{
    public ReadOnlySpan<byte> Name => "fetch"u8;
    private static Encoding Encoding => Encoding.Default;

    private static readonly string[] ResponseLinesUnknownService =
    [

    ];

    public async Task<HanldeResult> ProcessAsync(ReadOnlySequence<byte> arguments, CancellationToken cancellationToken)
    {
        var plugin = pluginProvider.Plugins.FirstOrDefault(
            plugin => string.Equals(Encoding.GetString(arguments), plugin.Name, StringComparison.Ordinal)
        );

        if (plugin == null)
        {
            var result = new HanldeResult
            {
                Lines = new List<string> { "# Unknown service", "." }, 
                Status = Status.Continue
            };
            return result;
        }

        var responseLines = new List<string>(capacity: plugin.Fields.Count + 1);

        foreach (var field in plugin.Fields)
        {
            var valueString = await field.GetFormattedValueStringAsync(
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            responseLines.Add($"{field.Name}.value {valueString}");
        }

        responseLines.Add(".");
        var result2 = new HanldeResult
        {
            Lines = responseLines,
            Status = Status.Continue
        };
        return result2;
    }
}