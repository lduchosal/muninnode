using System.Buffers;
using MuninNode.Server;

namespace MuninNode.Commands;

[TestClass]
[TestCategory("Command")]
public class ShortQuitCommandTest 
{
    [TestMethod]
    public async Task Test()
    {
        // Prepare
        var tokenSource = new CancellationTokenSource(200);
        var args = ReadOnlySequence<byte>.Empty;
        var command = new ShortQuitCommand();
        
        // Act
        var result = await command.ProcessAsync(args, tokenSource.Token);
        
        // Assert
        Assert.AreEqual(Status.Quit, result.Status);
        Assert.AreEqual(0, result.Lines.Count);
    }
}