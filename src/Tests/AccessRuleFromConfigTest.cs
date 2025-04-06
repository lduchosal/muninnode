using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MuninNode;
using MuninNode.AccessRules;

namespace Tests;

[TestClass]
public class AccessRuleFromConfigTest {
  
  [DataTestMethod]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "127.0.0.1:1233", true)]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "1.2.3.4:1233", true)]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "1.2.3.5:1233", false)]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "10.10.10.10:0", true)]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "5.6.7.8:9011", true)]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "0.0.0.0:9011", false)]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "0.0.0.1:9011", false)]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "0.0.1.0:9011", false)]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "0.1.1.0:9011", false)]
  [DataRow("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "1.1.1.0:9011", false)]
  public void Test(string allowFrom, string endpoint, bool result)
  {
    
    // Prepare

    var ipendpoint = IPEndPoint.Parse(endpoint);
    var appsettings = new Dictionary<string, string?>
    {
      {"MuninNode:AllowFrom", allowFrom},
    };
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(appsettings)
      .Build();

    var config = configuration.BuildMuninNodeConfig();
    var access = new AccessRuleFromConfig(config);
    
    // Act
    
    var acceptable = access.IsAcceptable(ipendpoint);
    
    // Assert

    Assert.AreEqual(result, acceptable);
  }
}
