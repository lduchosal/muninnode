# MuninNode

A C# implementation of a Munin Node service that allows for monitoring system and application metrics using the Munin protocol.

## Overview

MuninNode is a .NET library for creating Munin nodes that can expose metrics from your applications or systems. It implements the Munin protocol and provides a plugin-based architecture for extending functionality.

## Features

- Full implementation of the Munin Node protocol
- Plugin-based architecture for easy extension
- Support for custom data sources
- Configurable access rules
- Socket abstraction for flexibility in deployment

## Project Structure

- **Core**: Main implementation of the Munin Node service
- **Plugins**: Extensible plugin system for adding metrics
- **Commands**: Implementation of Munin protocol commands
- **SocketCreate**: Socket abstraction for network communication
- **AccessRules**: Security rules for controlling access to the node

## Getting Started

### Prerequisites

- .NET 6.0 or higher

### Installation

```bash
dotnet add package MuninNode
```

### Basic Usage

```csharp
// Create and configure a Munin node
var config = new MuninNodeConfiguration();
var muninNode = new MuninNode(config);

// Register plugins
muninNode.RegisterPlugin(new MyCustomPlugin());

// Start the node
muninNode.Start();
```

## Creating Plugins

To create a custom plugin:

1. Implement the `IPlugin` interface or extend the `Plugin` base class
2. Define fields using `IPluginField` implementations
3. Register your plugin with the MuninNode instance

```csharp
public class MyPlugin : Plugin
{
    public MyPlugin() : base("myplugin")
    {
        // Define fields
        AddField(new PluginFieldBase("mymetric", "My Metric"));
    }

    public override IEnumerable<string> GetConfig()
    {
        // Return Munin config
        yield return "graph_title My Custom Graph";
        yield return "graph_category custom";
    }

    public override void Update()
    {
        // Update field values
        Fields["mymetric"].Value = 42;
    }
}
```

## Configuration

MuninNode can be configured using the `MuninNodeConfiguration` class:

```csharp
var config = new MuninNodeConfiguration
{
    Port = 4949,
    Host = "0.0.0.0",
    // Other configuration options
};
```

## Security

The `AccessRules` namespace provides functionality to restrict access to your Munin node. Configure allowed clients:

```csharp
config.AccessRules.Add(new AccessRuleFromConfig
{
    AllowedIPs = new[] { "127.0.0.1", "10.0.0.0/24" }
});
```

## License

see LICENSE

## Contributing

see guidelines