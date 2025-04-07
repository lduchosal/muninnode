using System.Buffers;
using MuninNode.Plugins;
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
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(string.IsNullOrEmpty(result[0]), result[0]);
    }
}