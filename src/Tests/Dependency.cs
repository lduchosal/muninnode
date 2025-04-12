using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MuninNode;
using MuninNode.AccessRules;
using MuninNode.Commands;
using MuninNode.Plugins;
using MuninNode.Server;

namespace Tests;

public static class Dependency
{
    public static IServiceCollection AddMunin(
        this IServiceCollection services,
        string listen,
        int port,
        string hostname,
        string allowFrom)
    {
        var settings = new Dictionary<string, string?>
        {
            { "Core:Listen", $"{listen}" },
            { "Core:Port", $"{port}" },
            { "Core:Hostname", $"{hostname}" },
            { "Core:AllowFrom", $"{allowFrom}" },
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        services
            .AddLogging()

            .AddScoped<IMuninNode, MuninServer>()
            .AddScoped<IPluginProvider, EmptyPluginProvider>()
            .AddScoped<IAccessRule, AccessRuleFromConfig>()
            .AddScoped<MuninNodeConfiguration>(_ => configuration.BuildMuninNodeConfig())

            .AddScoped<MuninServer>()
            .AddScoped<SessionManager>()
            .AddScoped<SocketListener>()
            .AddScoped<MuninServer>()
            .AddScoped<CommunicationHandler>()
            .AddScoped<MuninProtocol>()
            .AddScoped<ICommand, QuitCommand>()
            .AddScoped<ICommand, ShortQuitCommand>()
            .AddScoped<ICommand, CapCommand>()
            .AddScoped<ICommand, ConfigCommand>()
            .AddScoped<ICommand, FetchCommand>()
            .AddScoped<ICommand, HelpCommand>()
            .AddScoped<ICommand, ListCommand>()
            .AddScoped<ICommand, NodeCommand>()
            .AddScoped<ICommand, VersionCommand>()
            .AddScoped<IDefaultCommand, HelpCommand>()
            ;

        return services;
    }
}

public class EmptyPluginProvider : IPluginProvider
{
    public IReadOnlyCollection<IPlugin> Plugins { get; } = [];
    public Task ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken) => Task.CompletedTask;

}