// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using MuninNode.Server;

namespace MuninNode.Plugins;

/// <summary>
/// Provides an interface that abstracts the plugin provider.
/// </summary>
public interface IPluginProvider : ISessionCallback
{
    /// <summary>Gets a readonly collection of <see cref="IPlugin"/> provided by this provider.</summary>
    /// <remarks>
    ///   <para>This property is referenced each time a during processing of a request session from the <c>munin-update</c>, such as fetching data or getting configurations.</para>
    ///   <para>The the collection returned from this property should not be changed during the processing of each request session.</para>
    /// </remarks>
    /// <seealso cref="IPlugin"/>
    /// <seealso cref="MuninServer"/>
    IReadOnlyCollection<IPlugin> Plugins { get; }
}