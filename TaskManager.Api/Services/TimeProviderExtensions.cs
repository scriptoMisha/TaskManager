namespace TaskManager.Api.Services;

public static class TimeProviderExtensions
{
    public static DateTime GetUtcNowTruncatedToMicroseconds(this TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
