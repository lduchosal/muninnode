using System.Buffers;
using MuninNode.Plugins;
using MuninNode.Server;
using Tests;

namespace MuninNode.Commands;

[TestClass]
[TestCategory("Command")]
public class ListCommandTest 
{
    [TestMethod]
    public async Task Test()
    {
        // Prepare
        var plugins = new EmptyPluginProvider();
        var tokenSource = new CancellationTokenSource(200);
        var args = ReadOnlySequence<byte>.Empty;
        var command = new ListCommand(plugins);
        
        // Act
        var result = await command.ProcessAsync(args, tokenSource.Token);
        
        // Assert
        Assert.AreEqual(Status.Continue, result.Status);
        Assert.AreEqual(1, result.Lines.Count);
        Assert.IsTrue(string.IsNullOrEmpty(result.Lines[0]), result.Lines[0]);
    }
}