using System.Buffers;
using MuninNode.Plugins;

namespace MuninNode.Commands;

public class ListCommand(IPluginProvider pluginProvider) : ICommand
{
    public ReadOnlySpan<byte> Name => "list"u8;

    public Task<HanldeResult> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        var plugins = string.Join(" ", pluginProvider.Plugins.Select(static plugin => plugin.Name));
        var result = new HanldeResult
        {
            Lines = [ plugins ],
            Status = Status.Continue
        };
        return Task.FromResult(result);
    }
}