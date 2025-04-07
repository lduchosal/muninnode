// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

public class Plugin : IPlugin, IDataSource, INodeSessionCallback
{
    public string Name { get; }

    public GraphAttributes GraphAttributes { get; }
    public IReadOnlyCollection<IField> Fields { get; }

#pragma warning disable CA1033
    IGraphAttributes IPlugin.GraphAttributes => GraphAttributes;

    IDataSource IPlugin.DataSource => this;

    IReadOnlyCollection<IField> IDataSource.Fields => Fields;

    INodeSessionCallback IPlugin.SessionCallback => this;
#pragma warning restore CA1033

    public Plugin(
        string name,
        GraphAttributes graphAttributes,
        IReadOnlyCollection<IField> fields
    )
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        Name = name;
        GraphAttributes = graphAttributes ?? throw new ArgumentNullException(nameof(graphAttributes));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    ValueTask INodeSessionCallback.ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
        => ReportSessionStartedAsync(sessionId, cancellationToken);

    protected virtual ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
        => default; // do nothing in this class

    ValueTask INodeSessionCallback.ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
        => ReportSessionClosedAsync(sessionId, cancellationToken);

    protected virtual ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
        => default; // do nothing in this class
}