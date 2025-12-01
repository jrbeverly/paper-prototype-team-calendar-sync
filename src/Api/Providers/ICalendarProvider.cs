namespace Api.Providers;

public interface ICalendarProvider
{
    Task<IReadOnlyList<string>> GetAttendeesAsync(string meetingId, CancellationToken cancellationToken = default);
    Task SetAttendeesAsync(string meetingId, IEnumerable<string> attendees, CancellationToken cancellationToken = default);
}
