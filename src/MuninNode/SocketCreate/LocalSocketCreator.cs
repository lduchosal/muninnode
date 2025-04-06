// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net;
using System.Net.Sockets;
using MuninNode.SocketCreate;

namespace MuninNode.Node;

/// <summary>
/// Implement a <c>Munin-Node</c> that acts as a node on the localhost and only accepts connections from the local loopback address (127.0.0.1, ::1).
/// </summary>
public class LocalSocketCreator : ISocketCreator
{
    private const string DefaultHostName = "munin-node.localhost";

    /// <summary>
    /// Gets the <see cref="EndPoint"/> to be bound as the <c>Munin-Node</c>'s endpoint.
    /// </summary>
    /// <returns>
    /// An <see cref="EndPoint"/>.
    /// The default implementation returns an <see cref="IPEndPoint"/> with the port number <c>0</c>
    /// and <see cref="IPAddress.IPv6Loopback"/>/<see cref="IPAddress.Loopback"/>.
    /// </returns>
    protected virtual EndPoint GetLocalEndPointToBind()
        => new IPEndPoint(
            address:
            Socket.OSSupportsIPv6
                ? IPAddress.IPv6Loopback
                : Socket.OSSupportsIPv4
                    ? IPAddress.Loopback
                    : throw new NotSupportedException(),
            port: 0
        );

    public Socket CreateServerSocket()
    {
        const int MaxClients = 1;

        Socket? server = null;

        try
        {
            var endPoint = GetLocalEndPointToBind();

            server = new Socket(
                endPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv4)
            {
                server.DualMode = true;
            }

            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            server.Bind(endPoint);
            server.Listen(MaxClients);

            return server;
        }
        catch
        {
            server?.Dispose();
            throw;
        }
    }
}