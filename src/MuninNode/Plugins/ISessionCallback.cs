// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace MuninNode.Plugins;

/// <summary>
/// Defines the callbacks when a request session from the <c>munin-update</c> starts or ends.
/// </summary>
public interface ISessionCallback
{
    
    /// <summary>
    /// Implements a callback to be called when <c>munin-update</c> starts a session.
    /// </summary>
    /// <remarks>This method is called back when the <see cref="MuninNode"/> starts processing a session.</remarks>
    /// <param name="sessionId">A unique ID that <see cref="MuninNode"/> associates with the session.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
    Task ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Implements a callback to be called when <c>munin-update</c> ends a session.
    /// </summary>
    /// <remarks>This method is called back when the <see cref="MuninNode"/> ends processing a session.</remarks>
    /// <param name="sessionId">A unique ID that <see cref="MuninNode"/> associates with the session.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
    Task ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken);
}