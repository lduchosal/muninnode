using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MuninNode;

namespace Tests;

[TestClass]
public class MuninNodeTest
{

    [TestMethod]
    public async Task Test()
    {

        // Prepare

        var services = new ServiceCollection();
        services.AddMunin(
            listen: "127.0.0.1",
            port: 14949,
            hostname: "localhost",
            allowFrom: "127.0.0.1"
        );

        var provider = services.BuildServiceProvider();
        var muninNode = provider.GetRequiredService<IMuninNode>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act

        await muninNode.RunAsync(cts.Token);

        // Assert

        Assert.IsTrue(true);
    }
}
