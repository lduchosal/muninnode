using System.Net;
using Microsoft.Extensions.Configuration;
using MuninNode;
using MuninNode.SocketCreate;

namespace Tests;

[TestClass]
public class SocketCreatorTest
{
    [DataTestMethod]
    [DataRow("127.0.0.1", 14949)]
    [DataRow("0.0.0.0", 14945)]
    public void Test(string listen, int port)
    {

        // Prepare

        var appsettings = new Dictionary<string, string?>
        {
            { "MuninNode:Listen", listen },
            { "MuninNode:Port", $"{port}" },
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(appsettings)
            .Build();

        var config = configuration.BuildMuninNodeConfig();
        ISocketCreator scoketCreator = new SocketCreator(config);

        // Act

        using var socket = scoketCreator.CreateServerSocket();

        // Assert

        Assert.AreEqual(false, socket.Connected);
        Assert.IsInstanceOfType<IPEndPoint>(socket.LocalEndPoint);
        Assert.AreEqual(port, ((IPEndPoint)socket.LocalEndPoint).Port);
        Assert.AreEqual(listen, ((IPEndPoint)socket.LocalEndPoint).Address.ToString());
    }
}
