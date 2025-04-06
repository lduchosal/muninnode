using System.Buffers;
using System.Threading.Tasks;
using MuninNode.Plugins;

namespace MuninNode.Commands;

public class ListCommand(IPluginProvider pluginProvider) : ICommand
{
    public Task<string> ProcessAsync(ReadOnlySequence<byte> args)
    {
        var result = string.Join(" ", pluginProvider.Plugins.Select(static plugin => plugin.Name));
        return Task.FromResult(result);
    }

}