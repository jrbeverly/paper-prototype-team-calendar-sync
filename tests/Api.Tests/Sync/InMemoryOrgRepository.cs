using Api.Org;

namespace Api.Tests.Sync;

internal sealed class InMemoryOrgRepository : IOrgRepository
{
    private readonly List<OrgTeam> _teams = [];
    private readonly List<TeamMember> _members = [];
    private readonly List<TeamObserver> _observers = [];
    private readonly List<RecurringMeeting> _meetings = [];

    public Task<IReadOnlyList<OrgTeam>> GetTeamsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<OrgTeam>>(_teams);

    public Task<IReadOnlyList<TeamMember>> GetMembersAsync(string teamId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TeamMember>>(
            _members.Where(m => m.TeamId == teamId).ToList());

    public Task<IReadOnlyList<TeamObserver>> GetObserversAsync(string teamId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TeamObserver>>(
            _observers.Where(o => o.TeamId == teamId).ToList());

    public Task<IReadOnlyList<RecurringMeeting>> GetMeetingsAsync(string teamId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RecurringMeeting>>(
            _meetings.Where(m => m.TeamId == teamId).ToList());

    public Task SaveTeamAsync(OrgTeam team, CancellationToken ct = default)
    {
        _teams.Add(team);
        return Task.CompletedTask;
    }

    public Task SaveMemberAsync(TeamMember member, CancellationToken ct = default)
    {
        _members.Add(member);
        return Task.CompletedTask;
    }

    public Task SaveObserverAsync(TeamObserver observer, CancellationToken ct = default)
    {
        _observers.Add(observer);
        return Task.CompletedTask;
    }

    public Task SaveMeetingAsync(RecurringMeeting meeting, CancellationToken ct = default)
    {
        _meetings.Add(meeting);
        return Task.CompletedTask;
    }
}
