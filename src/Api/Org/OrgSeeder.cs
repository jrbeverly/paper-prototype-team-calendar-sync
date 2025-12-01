namespace Api.Org;

public static class OrgSeeder
{
    public static readonly IReadOnlyList<OrgTeam> Teams =
    [
        new("platform-engineering", "Platform Engineering", "Builds and maintains shared infrastructure, tooling, and internal platforms."),
        new("product-design", "Product Design", "Designs user experiences and maintains the design system."),
    ];

    public static readonly IReadOnlyList<TeamMember> Members =
    [
        new("platform-engineering", "alice@example.com", "Alice Chen", "Tech Lead"),
        new("platform-engineering", "bob@example.com", "Bob Patel", "Senior Engineer"),
        new("platform-engineering", "carol@example.com", "Carol Kim", "Engineer"),
        new("product-design", "dan@example.com", "Dan Rivera", "Lead Designer"),
        new("product-design", "eve@example.com", "Eve Thompson", "Product Designer"),
        new("product-design", "frank@example.com", "Frank Liu", "UX Researcher"),
    ];

    public static readonly IReadOnlyList<TeamObserver> Observers =
    [
        new("platform-engineering", "dan@example.com", "Dan Rivera"),
        new("platform-engineering", "grace@example.com", "Grace Okonkwo"),
        new("product-design", "alice@example.com", "Alice Chen"),
        new("product-design", "grace@example.com", "Grace Okonkwo"),
    ];

    public static readonly IReadOnlyList<RecurringMeeting> Meetings =
    [
        new("pe-standup", "platform-engineering", "Daily Standup", "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR", ObserverParticipation.ReadOnly),
        new("pe-sprint-planning", "platform-engineering", "Sprint Planning", "FREQ=WEEKLY;INTERVAL=2;BYDAY=MO", ObserverParticipation.Invited),
        new("pe-retrospective", "platform-engineering", "Sprint Retrospective", "FREQ=WEEKLY;INTERVAL=2;BYDAY=FR", ObserverParticipation.Invited),
        new("pe-architecture-review", "platform-engineering", "Architecture Review", "FREQ=WEEKLY;BYDAY=WE", ObserverParticipation.Invited),
        new("pd-standup", "product-design", "Daily Standup", "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR", ObserverParticipation.ReadOnly),
        new("pd-design-review", "product-design", "Design Review", "FREQ=WEEKLY;BYDAY=TH", ObserverParticipation.Invited),
        new("pd-retrospective", "product-design", "Sprint Retrospective", "FREQ=WEEKLY;INTERVAL=2;BYDAY=FR", ObserverParticipation.Invited),
    ];

    public static async Task SeedAsync(IOrgRepository repo, CancellationToken ct = default)
    {
        foreach (var team in Teams)
            await repo.SaveTeamAsync(team, ct);
        foreach (var member in Members)
            await repo.SaveMemberAsync(member, ct);
        foreach (var observer in Observers)
            await repo.SaveObserverAsync(observer, ct);
        foreach (var meeting in Meetings)
            await repo.SaveMeetingAsync(meeting, ct);
    }
}
