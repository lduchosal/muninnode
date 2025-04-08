using Plugins;

namespace Tests.Plugins;

[TestClass]
public class UptimeTest
{
    [TestMethod]
    public async Task Test()
    {
        // Prepare
        var uptime = new Uptime("localhost");
        var cancel = new CancellationTokenSource(10).Token;
        
        // Act
        var value = await uptime.Fields.First().GetFormattedValueStringAsync(cancel);
        
        // Assert
        Assert.IsNotNull(value);
    }
}