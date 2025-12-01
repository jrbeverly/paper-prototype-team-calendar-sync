namespace Api.Providers;

public sealed class MockCalendarProvider : ICalendarProvider
{
    private readonly Dictionary<string, HashSet<string>> _state = new();
    private readonly object _gate = new();

    public Task<IReadOnlyList<string>> GetAttendeesAsync(string meetingId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            IReadOnlyList<string> result = _state.TryGetValue(meetingId, out var set)
                ? [.. set.Order(StringComparer.OrdinalIgnoreCase)]
                : [];
            return Task.FromResult(result);
        }
    }

    public Task SetAttendeesAsync(string meetingId, IEnumerable<string> attendees, CancellationToken cancellationToken = default)
    {
        lock (_gate)
            _state[meetingId] = new HashSet<string>(attendees, StringComparer.OrdinalIgnoreCase);
        return Task.CompletedTask;
    }
}
