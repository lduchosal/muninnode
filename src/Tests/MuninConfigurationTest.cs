using System.Net;
using Microsoft.Extensions.Configuration;
using MuninNode;

namespace Tests;

[TestClass]
public class MuninNodeConfigurationTest {
  
  [TestMethod]
  public void TestDefaultValues()
  {
    
    // Prepare
    
    // Act
    
    var config = new MuninNodeConfiguration();
    
    // Assert

    Assert.AreEqual(4949, config.Port);
    Assert.AreEqual(IPAddress.Loopback, config.Listen);
    Assert.AreEqual("localhost", config.Hostname);
    Assert.AreEqual(2, config.AllowFrom.Count);
  }
  
  [TestMethod]
  public void TestValues()
  {
    
    // Prepare
    // Act

    var config = new MuninNodeConfiguration {
      Hostname = "hostname",
      Port = 1234,
      Listen = IPAddress.Any,
      AllowFrom = [IPAddress.Any]
    };
    
    // Assert

    Assert.AreEqual(1234, config.Port);
    Assert.AreEqual(IPAddress.Any, config.Listen);
    Assert.AreEqual("hostname", config.Hostname);
    Assert.AreEqual(1, config.AllowFrom.Count);
  }
  
  [TestMethod]
  public void TestFromConfiguration()
  {
    
    // Prepare

    var appsettings = new Dictionary<string, string?>
    {
      {"Core:Port", "9876"},
      {"Core:Hostname", "appsettings"},
      {"Core:Listen", "::1"},
      {"Core:AllowFrom", "127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10"}
    };

    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(appsettings)
      .Build();

    // Act

    var config = configuration.BuildMuninNodeConfig();
    
    // Assert

    Assert.AreEqual(9876, config.Port);
    Assert.AreEqual(IPAddress.Parse("::1"), config.Listen);
    Assert.AreEqual("appsettings", config.Hostname);
    Assert.AreEqual(4, config.AllowFrom.Count);
  }
}
