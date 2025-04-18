using Microsoft.Extensions.DependencyInjection;
using MuninNode;

namespace Tests;

[TestClass]
public class MuninServerTest
{

    [TestMethod]
    public async Task Test()
    {

        // Prepare

        var services = new ServiceCollection()
            .AddMunin(
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
