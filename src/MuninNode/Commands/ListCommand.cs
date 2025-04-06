using System.Buffers;
using MuninNode.Plugins;

namespace MuninNode.Commands;

public class ListCommand(IPluginProvider pluginProvider) : ICommand
{
    public ReadOnlySpan<byte> Name => "list"u8;

    public Task<string[]> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        var result = string.Join(" ", pluginProvider.Plugins.Select(static plugin => plugin.Name));
        return Task.FromResult<string[]>([result]);
    }
}