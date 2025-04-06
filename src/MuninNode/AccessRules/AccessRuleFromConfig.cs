using System.Net;

namespace MuninNode.AccessRules;

public sealed class AccessRuleFromConfig(MuninNodeConfiguration config) : IAccessRule {
  public bool IsAcceptable(IPEndPoint remoteEndPoint) =>
    config.AllowFrom.Any(
      ip => ip.ToString() == remoteEndPoint.Address.ToString());
}
