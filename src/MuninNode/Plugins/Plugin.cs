// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

public class Plugin : IPlugin, ISessionCallback
{
    public string Name { get; }
    public GraphAttributes GraphAttributes { get; }
    public IReadOnlyCollection<IField> Fields { get; }

    IGraphAttributes IPlugin.GraphAttributes => GraphAttributes;
    IReadOnlyCollection<IField> IPlugin.Fields => Fields;

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

    Task ISessionCallback.ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
        => ReportSessionStartedAsync(sessionId, cancellationToken);

    protected virtual Task ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
        => Task.CompletedTask; // do nothing in this class

    Task ISessionCallback.ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
        => ReportSessionClosedAsync(sessionId, cancellationToken);

    protected virtual Task ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
        => Task.CompletedTask; // do nothing in this class
}