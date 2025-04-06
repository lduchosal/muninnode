using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace MuninNode;

public sealed class MuninNodeConfiguration
{
    public IPAddress Listen { get; init; } = IPAddress.Loopback;
    public int Port { get; init; } = 4949;
    public string Hostname { get; init; } = "localhost";
    public List<IPAddress> AllowFrom { get; init; } = [ IPAddress.Loopback, IPAddress.IPv6Loopback ];
}

public static class IConfigurationExt
{
    public static MuninNodeConfiguration BuildMuninNodeConfig(this IConfiguration? configuration)
    {
        return new MuninNodeConfiguration
        {
            Port = configuration?["MuninNode:Port"]?.ToInt() ?? 4949,
            Listen = configuration?["MuninNode:Listen"]?.ToIPAddress() ?? IPAddress.Loopback,
            Hostname = configuration?["MuninNode:Hostname"] ?? "localhost",
            AllowFrom = configuration?["MuninNode:AllowFrom"]?.ToNetworkList() ??
            [IPAddress.Loopback, IPAddress.IPv6Loopback],
        };
    }
}

public static class StringExt
{
    public static int? ToInt(this string s)
    {
        int.TryParse(s, out var value);
        return value;
    }

    public static List<IPAddress> ToNetworkList(this string s)
    {
        var result = s.Split([' ', ',', ';'])
                .Select(s =>
                {
                    bool parsed = IPAddress.TryParse(s, out var ipAddress);
                    return new
                    {
                        IP = ipAddress,
                        Parsed = parsed,
                    };
                })
                .Where(p => p.Parsed)
                .Select(p => p.IP!)
                .ToList()
            ;
        return result;
    }

    public static IPAddress? ToIPAddress(this string s)
    {
        IPAddress.TryParse(s, out var ipAddress);
        return ipAddress;
    }
}
