using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MuninNode;
using MuninNode.AccessRules;
using MuninNode.Commands;
using MuninNode.Plugins;

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
            { "MuninNode:Listen", $"{listen}" },
            { "MuninNode:Port", $"{port}" },
            { "MuninNode:Hostname", $"{hostname}" },
            { "MuninNode:AllowFrom", $"{allowFrom}" },
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        services
            .AddLogging()

            .AddScoped<IMuninNode, MuninNode.MuninNode>()
            .AddScoped<IPluginProvider, EmptyPluginProvider>()
            .AddScoped<IAccessRule, AccessRuleFromConfig>()
            .AddScoped<MuninNodeConfiguration>(_ => configuration.BuildMuninNodeConfig())

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