using System.Buffers;

namespace MuninNode.Commands;

[TestClass]
[TestCategory("Command")]
public class VersionCommandTest 
{
    [TestMethod]
    public async Task Test()
    {
        // Prepare
        var config = new MuninNodeConfiguration
        {
            Hostname = "localhost",
        };
        var tokenSource = new CancellationTokenSource(200);
        var args = ReadOnlySequence<byte>.Empty;
        var command = new VersionCommand(config);
        
        // Act
        var result = await command.ProcessAsync(args, tokenSource.Token);
        
        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].Contains(config.Hostname), result[0]);
    }
}