using MuninNode.Plugins;

namespace Plugins;

public class Uptime(string hostname) : IPlugin
{
    private DateTime startAt = DateTime.Now;
    public string Name => "uptime";
    public IGraphAttributes GraphAttributes
        => new GraphAttributes(
            category: WellKnownCategories.System,
            title: $"Uptime of {hostname}",
            verticalLabel: "Uptime [minutes]",
            scale: false,
            arguments: "--base 1000 --lower-limit 0"
        );
    public IReadOnlyCollection<FieldBase> Fields => new[]
    {
        new FieldBase
        {
            Name = "uptime",
            Label = hostname,
            GraphStyle = GraphStyle.Area,
            WarningRange = ValueRange.None,
            CriticalRange = ValueRange.None,
            // Set the number of minutes elapsed from the start time of the process as the 'uptime' value.
            FetchValue = () => (DateTime.Now - startAt).TotalMinutes

        }
    };
    public Task ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken) => Task.CompletedTask;

}