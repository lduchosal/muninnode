// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

public class Plugin : IPlugin, ISessionCallback
{
    public string Name { get; }
    public GraphAttributes GraphAttributes { get; }
    public IReadOnlyCollection<FieldBase> Fields { get; }

    IGraphAttributes IPlugin.GraphAttributes => GraphAttributes;
    IReadOnlyCollection<FieldBase> IPlugin.Fields => Fields;

    public Plugin(
        string name,
        GraphAttributes graphAttributes,
        IReadOnlyCollection<FieldBase> fields
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        Name = name;
        GraphAttributes = graphAttributes;
        Fields = fields;
    }

    Task ISessionCallback.ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
        => Task.CompletedTask;

    Task ISessionCallback.ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
        => Task.CompletedTask;
}