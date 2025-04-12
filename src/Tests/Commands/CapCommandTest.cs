using System.Buffers;
using MuninNode.Server;

namespace MuninNode.Commands;

[TestClass]
[TestCategory("Command")]
public class CapCommandTest 
{
    [TestMethod]
    public async Task Test()
    {
        // Prepare
        var tokenSource = new CancellationTokenSource(200);
        var args = ReadOnlySequence<byte>.Empty;
        var command = new CapCommand();
        
        // Act
        var result = await command.ProcessAsync(args, tokenSource.Token);
        
        // Assert
        Assert.AreEqual(Status.Continue, result.Status);
        Assert.AreEqual(1, result.Lines.Count);
        Assert.IsTrue(result.Lines[0].Contains("cap"), result.Lines[0]);
    }
}