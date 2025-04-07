using System.Buffers;

namespace MuninNode.Commands;

[TestClass]
[TestCategory("Command")]
public class NodeCommandTest 
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
        
        var command = new NodeCommand(config);

        // Act
        var result = await command.ProcessAsync(args, tokenSource.Token);
        
        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.IsTrue(result[0].Contains(config.Hostname));
        Assert.IsTrue(result[1].Contains("."));
    }
}