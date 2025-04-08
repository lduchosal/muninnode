// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

/// <summary>
/// Provides an interface that abstracts the plugin.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html">Plugin reference</seealso>
public interface IPlugin : ISessionCallback
{
    /// <summary>Gets a plugin name.</summary>
    /// <remarks>This value is used as the plugin name returned by the 'list' argument, or the plugin name specified by the 'fetch' argument.</remarks>
    string Name { get; }

    /// <summary>Gets a collection of plugin fields (<see cref="IField"/>) provided by this data source.</summary>
    /// <seealso cref="IField"/>
    IReadOnlyCollection<IField> Fields { get; }
    
    /// <summary>Gets a <see cref="IGraphAttributes"/> that represents the graph attributes when the field values (<see cref="IField"/>) are drawn as a graph.</summary>
    /// <seealso cref="IGraphAttributes"/>
    /// <seealso cref="Plugins.GraphAttributes"/>
    IGraphAttributes GraphAttributes { get; }

}