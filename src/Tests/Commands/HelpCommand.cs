using System.Buffers;

namespace MuninNode.Commands;

[TestClass]
[TestCategory("Command")]
public class HelpCommandTest 
{
    [TestMethod]
    public async Task Test()
    {
        // Prepare
        var tokenSource = new CancellationTokenSource(200);
        var args = ReadOnlySequence<byte>.Empty;
        var command = new HelpCommand();
        
        // Act
        var result = await command.ProcessAsync(args, tokenSource.Token);
        
        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].Contains("Unknown command"), result[0]);
    }
}