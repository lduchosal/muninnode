// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

/// <summary>
/// Provides an interface that abstracts the data source for the plugin.
/// </summary>
public interface IDataSource
{
    /// <summary>Gets a collection of plugin fields (<see cref="IField"/>) provided by this data source.</summary>
    /// <seealso cref="IField"/>
    IReadOnlyCollection<IField> Fields { get; }
}