// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

/// <summary>
/// Provides an interface that abstracts the plugin.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html">Plugin reference</seealso>
public interface IPlugin
{
    /// <summary>Gets a plugin name.</summary>
    /// <remarks>This value is used as the plugin name returned by the 'list' argument, or the plugin name specified by the 'fetch' argument.</remarks>
    string Name { get; }

    /// <summary>Gets a <see cref="IGraphAttributes"/> that represents the graph attributes when the field values (<see cref="IField"/>) are drawn as a graph.</summary>
    /// <seealso cref="IGraphAttributes"/>
    /// <seealso cref="Plugins.GraphAttributes"/>
    IGraphAttributes GraphAttributes { get; }

    /// <summary>Gets a <see cref="IDataSource"/> that serves as the data source for the plugin.</summary>
    /// <seealso cref="IDataSource"/>
    IDataSource DataSource { get; }

    /// <summary>Gets a <see cref="INodeSessionCallback"/>, which defines the callbacks when a request session from the <c>munin-update</c> starts or ends, such as fetching data or getting configurations.</summary>
    /// <remarks>Callbacks of this interface can be used to initiate bulk collection of field values.</remarks>
    /// <seealso cref="INodeSessionCallback"/>
    /// <seealso cref="MuninNode"/>
    INodeSessionCallback SessionCallback { get; }
}