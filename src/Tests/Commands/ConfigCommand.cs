using System.Buffers;
using System.Text;
using MuninNode.Plugins;
using Tests;

namespace MuninNode.Commands;

[TestClass]
[TestCategory("Command")]
public class ConfigCommandTest 
{
    [TestMethod]
    public async Task Test()
    {
        // Prepare
        var plugins = new EmptyPluginProvider();
        var tokenSource = new CancellationTokenSource(200);
        var args = ReadOnlySequence<byte>.Empty;
        var command = new ConfigCommand(plugins);
        
        // Act
        var result = await command.ProcessAsync(args, tokenSource.Token);
        
        // Assert
        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.IsTrue(result[0].Contains("# Unknown service"), result[0]);
        Assert.IsTrue(result[1].Contains("."), result[1]);
    }
}