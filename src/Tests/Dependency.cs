using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MuninNode;
using MuninNode.AccessRules;
using MuninNode.Commands;
using MuninNode.Plugins;
using MuninNode.SocketCreate;

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
            .AddScoped<ISocketCreator, SocketCreator>()
            .AddScoped<IPluginProvider, EmptyPluginProvider>()
            .AddScoped<IAccessRule, AccessRuleFromConfig>()
            .AddScoped<MuninNodeConfiguration>(_ => configuration.BuildMuninNodeConfig())

            .AddScoped<CapCommand>()
            .AddScoped<ConfigCommand>()
            .AddScoped<FetchCommand>()
            .AddScoped<HelpCommand>()
            .AddScoped<ListCommand>()
            .AddScoped<NodeCommand>()
            .AddScoped<VersionCommand>()
            ;

        return services;
    }
}

public class EmptyPluginProvider(INodeSessionCallback sessionCallback) : IPluginProvider
{
    public IReadOnlyCollection<IPlugin> Plugins { get; } = [];
    public INodeSessionCallback SessionCallback { get; } = sessionCallback;
}