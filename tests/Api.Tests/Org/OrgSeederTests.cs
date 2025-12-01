using Api.Org;
using Xunit;

namespace Api.Tests.Org;

public class OrgSeederTests
{
    [Fact]
    public void Teams_AreNonEmpty() => Assert.NotEmpty(OrgSeeder.Teams);

    [Fact]
    public void Teams_HaveUniqueIds()
    {
        var ids = OrgSeeder.Teams.Select(t => t.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void Members_AllReferenceDefinedTeams()
    {
        var teamIds = OrgSeeder.Teams.Select(t => t.Id).ToHashSet();
        Assert.All(OrgSeeder.Members, m => Assert.Contains(m.TeamId, teamIds));
    }

    [Fact]
    public void Observers_AllReferenceDefinedTeams()
    {
        var teamIds = OrgSeeder.Teams.Select(t => t.Id).ToHashSet();
        Assert.All(OrgSeeder.Observers, o => Assert.Contains(o.TeamId, teamIds));
    }

    [Fact]
    public void Meetings_AllReferenceDefinedTeams()
    {
        var teamIds = OrgSeeder.Teams.Select(t => t.Id).ToHashSet();
        Assert.All(OrgSeeder.Meetings, m => Assert.Contains(m.TeamId, teamIds));
    }

    [Fact]
    public void Meetings_HaveUniqueIds()
    {
        var ids = OrgSeeder.Meetings.Select(m => m.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void Meetings_RecurrenceRulesStartWithFreq()
    {
        Assert.All(OrgSeeder.Meetings, m => Assert.StartsWith("FREQ=", m.Recurrence));
    }

    [Fact]
    public void EachTeam_HasAtLeastOneMember()
    {
        var memberTeamIds = OrgSeeder.Members.Select(m => m.TeamId).ToHashSet();
        Assert.All(OrgSeeder.Teams, t => Assert.Contains(t.Id, memberTeamIds));
    }

    [Fact]
    public void EachTeam_HasAtLeastOneMeeting()
    {
        var meetingTeamIds = OrgSeeder.Meetings.Select(m => m.TeamId).ToHashSet();
        Assert.All(OrgSeeder.Teams, t => Assert.Contains(t.Id, meetingTeamIds));
    }
}
