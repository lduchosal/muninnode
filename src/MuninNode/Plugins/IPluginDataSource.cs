// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Collections.Generic;

namespace MuninNode.Plugins;

/// <summary>
/// Provides an interface that abstracts the data source for the plugin.
/// </summary>
public interface IPluginDataSource {
  /// <summary>Gets a collection of plugin fields (<see cref="IPluginField"/>) provided by this data source.</summary>
  /// <seealso cref="IPluginField"/>
  IReadOnlyCollection<IPluginField> Fields { get; }
}
