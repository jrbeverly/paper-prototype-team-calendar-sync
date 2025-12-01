using Api.Org;
using Api.Providers;

namespace Api.Sync;

public sealed class SyncEngine(IOrgRepository repo, ICalendarProvider provider)
{
    public async Task<SyncResult> RunAsync(CancellationToken ct = default)
    {
        var teams = await repo.GetTeamsAsync(ct);
        var meetingsUpdated = 0;

        foreach (var team in teams)
        {
            var members = await repo.GetMembersAsync(team.Id, ct);
            var observers = await repo.GetObserversAsync(team.Id, ct);
            var meetings = await repo.GetMeetingsAsync(team.Id, ct);

            foreach (var meeting in meetings)
            {
                var desired = AttendancePolicy.DeriveAttendees(members, observers, meeting.ObserverParticipation);
                var actual = await provider.GetAttendeesAsync(meeting.Id, ct);

                if (!desired.SequenceEqual(actual, StringComparer.OrdinalIgnoreCase))
                {
                    await provider.SetAttendeesAsync(meeting.Id, desired, ct);
                    meetingsUpdated++;
                }
            }
        }

        return new SyncResult(meetingsUpdated);
    }
}
