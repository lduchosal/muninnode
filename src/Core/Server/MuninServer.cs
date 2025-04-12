// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace MuninNode.Server;

/// <summary>
/// Provides an extensible base class with basic Munin-Node functionality.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/node/index.html">The Munin node</seealso>
public sealed class MuninServer(
    ILogger<MuninServer> logger,
    SessionManager sessionManager)
    : IMuninNode
{
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"starting");
        await sessionManager.AcceptAsync(false, stoppingToken);
    }
}