namespace Api.Org;

public interface IOrgRepository
{
    Task<IReadOnlyList<OrgTeam>> GetTeamsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TeamMember>> GetMembersAsync(string teamId, CancellationToken ct = default);
    Task<IReadOnlyList<TeamObserver>> GetObserversAsync(string teamId, CancellationToken ct = default);
    Task<IReadOnlyList<RecurringMeeting>> GetMeetingsAsync(string teamId, CancellationToken ct = default);
    Task SaveTeamAsync(OrgTeam team, CancellationToken ct = default);
    Task SaveMemberAsync(TeamMember member, CancellationToken ct = default);
    Task SaveObserverAsync(TeamObserver observer, CancellationToken ct = default);
    Task SaveMeetingAsync(RecurringMeeting meeting, CancellationToken ct = default);
}
