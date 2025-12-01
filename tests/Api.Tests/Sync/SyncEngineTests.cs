using Api.Org;
using Api.Providers;
using Api.Sync;
using Xunit;

namespace Api.Tests.Sync;

public class SyncEngineTests
{
    private static async Task<(SyncEngine Engine, MockCalendarProvider Provider, InMemoryOrgRepository Repo)> BuildSeededAsync()
    {
        var repo = new InMemoryOrgRepository();
        await OrgSeeder.SeedAsync(repo);
        var provider = new MockCalendarProvider();
        return (new SyncEngine(repo, provider), provider, repo);
    }

    [Fact]
    public async Task Run_PopulatesProviderForAllSeededMeetings()
    {
        var (engine, provider, _) = await BuildSeededAsync();

        await engine.RunAsync();

        foreach (var meeting in OrgSeeder.Meetings)
            Assert.NotEmpty(await provider.GetAttendeesAsync(meeting.Id));
    }

    [Fact]
    public async Task Run_ReturnsUpdatedCountEqualToMeetingCount_OnFirstRun()
    {
        var (engine, _, _) = await BuildSeededAsync();

        var result = await engine.RunAsync();

        Assert.Equal(OrgSeeder.Meetings.Count, result.MeetingsUpdated);
    }

    [Fact]
    public async Task Run_ReadOnlyMeeting_ContainsMembersOnly()
    {
        var (engine, provider, _) = await BuildSeededAsync();
        await engine.RunAsync();

        var attendees = await provider.GetAttendeesAsync("pe-standup");

        var expected = AttendancePolicy.DeriveAttendees(
            OrgSeeder.Members.Where(m => m.TeamId == "platform-engineering").ToList(),
            OrgSeeder.Observers.Where(o => o.TeamId == "platform-engineering").ToList(),
            ObserverParticipation.ReadOnly);
        Assert.Equal(expected, attendees);
    }

    [Fact]
    public async Task Run_InvitedMeeting_ContainsMembersAndObservers()
    {
        var (engine, provider, _) = await BuildSeededAsync();
        await engine.RunAsync();

        var attendees = await provider.GetAttendeesAsync("pe-sprint-planning");

        var expected = AttendancePolicy.DeriveAttendees(
            OrgSeeder.Members.Where(m => m.TeamId == "platform-engineering").ToList(),
            OrgSeeder.Observers.Where(o => o.TeamId == "platform-engineering").ToList(),
            ObserverParticipation.Invited);
        Assert.Equal(expected, attendees);
    }

    [Fact]
    public async Task SecondRun_WithUnchangedOrg_IsIdempotent()
    {
        var (engine, _, _) = await BuildSeededAsync();
        await engine.RunAsync();

        var result = await engine.RunAsync();

        Assert.Equal(0, result.MeetingsUpdated);
    }

    [Fact]
    public async Task Run_AfterMemberAdded_ConvergesAffectedMeetings()
    {
        var (engine, provider, repo) = await BuildSeededAsync();
        await engine.RunAsync();

        await repo.SaveMemberAsync(new TeamMember("platform-engineering", "new@example.com", "New Person", "Engineer"));
        var result = await engine.RunAsync();

        var peMeetingCount = OrgSeeder.Meetings.Count(m => m.TeamId == "platform-engineering");
        Assert.Equal(peMeetingCount, result.MeetingsUpdated);
        Assert.Contains("new@example.com", await provider.GetAttendeesAsync("pe-standup"), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Run_AfterMemberAdded_UnaffectedTeamMeetingsAreNotUpdated()
    {
        var (engine, _, repo) = await BuildSeededAsync();
        await engine.RunAsync();

        await repo.SaveMemberAsync(new TeamMember("platform-engineering", "new@example.com", "New Person", "Engineer"));
        var result = await engine.RunAsync();

        var pdMeetingCount = OrgSeeder.Meetings.Count(m => m.TeamId == "product-design");
        Assert.Equal(OrgSeeder.Meetings.Count(m => m.TeamId == "platform-engineering"), result.MeetingsUpdated);
        // product-design meetings should not have been counted
        Assert.True(result.MeetingsUpdated < OrgSeeder.Meetings.Count);
        _ = pdMeetingCount; // used implicitly via the assertion above
    }

    [Fact]
    public async Task ThirdRunAfterMemberAdded_IsIdempotent()
    {
        var (engine, _, repo) = await BuildSeededAsync();
        await engine.RunAsync();
        await repo.SaveMemberAsync(new TeamMember("platform-engineering", "new@example.com", "New Person", "Engineer"));
        await engine.RunAsync();

        var result = await engine.RunAsync();

        Assert.Equal(0, result.MeetingsUpdated);
    }
}
