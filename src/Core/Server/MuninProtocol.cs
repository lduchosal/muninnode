using System.Buffers;
using MuninNode.Commands;
using MuninNode.Plugins;

namespace MuninNode.Server;

public class HanldeResult
{
    public List<string> Lines { get; init; } = new ();
    public Status Status { get; init; } = Status.Quit;
}

public enum Status
{
    Quit,
    Continue
}

public class MuninProtocol(
    MuninNodeConfiguration config,
    IPluginProvider pluginProvider,
    IEnumerable<ICommand> commands,
    IDefaultCommand help
)
{
    public List<string> GetBanner()
    {
        return [$"# munin node at {config.Hostname}", "\n"];
    }

    public async Task<HanldeResult> HandleCommandAsync(
        ReadOnlySequence<byte> commandLine,
        CancellationToken cancellationToken)
    {

        foreach (var command in commands)
        {
            if (commandLine.Expect(command.Name, out var args))
            {
                return await command.ProcessAsync(args, cancellationToken);
            }
        }
        
        return await help.ProcessAsync(ReadOnlySequence<byte>.Empty, cancellationToken);
    }

    public async Task SessionStartedAsync(string sessionId, CancellationToken cancellationToken)
    {
        await pluginProvider
                .ReportSessionStartedAsync(sessionId, cancellationToken)
                .ConfigureAwait(false)
            ;

        foreach (var plugin in pluginProvider.Plugins)
        {
            await plugin
                    .ReportSessionStartedAsync(sessionId, cancellationToken)
                    .ConfigureAwait(false)
                ;
        }
    }

    public async Task SessionClosedAsync(string sessionId, CancellationToken cancellationToken)
    {

        foreach (var plugin in pluginProvider.Plugins)
        {
            await plugin
                    .ReportSessionClosedAsync(sessionId, cancellationToken)
                    .ConfigureAwait(false)
                ;
        }

        await pluginProvider
                .ReportSessionClosedAsync(sessionId, cancellationToken)
                .ConfigureAwait(false)
            ;
    }
}