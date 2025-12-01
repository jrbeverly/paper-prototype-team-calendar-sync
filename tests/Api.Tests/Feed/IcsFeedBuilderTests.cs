using Api.Feed;
using Api.Org;
using Xunit;

namespace Api.Tests.Feed;

public class IcsFeedBuilderTests
{
    private static readonly RecurringMeeting Standup =
        new("pe-standup", "platform-engineering", "Daily Standup", "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR", ObserverParticipation.ReadOnly);

    private static readonly IReadOnlyList<string> TwoAttendees = ["alice@example.com", "bob@example.com"];

    [Fact]
    public void Build_EmptyEntries_ReturnsValidCalendarShell()
    {
        var result = IcsFeedBuilder.Build([]);

        Assert.Contains("BEGIN:VCALENDAR", result);
        Assert.Contains("END:VCALENDAR", result);
    }

    [Fact]
    public void Build_ContainsRequiredVersion()
    {
        var result = IcsFeedBuilder.Build([]);

        Assert.Contains("VERSION:2.0", result);
    }

    [Fact]
    public void Build_SingleMeeting_WrapsInVEvent()
    {
        var result = IcsFeedBuilder.Build([(Standup, TwoAttendees)]);

        Assert.Contains("BEGIN:VEVENT", result);
        Assert.Contains("END:VEVENT", result);
    }

    [Fact]
    public void Build_SingleMeeting_ContainsSummary()
    {
        var result = IcsFeedBuilder.Build([(Standup, TwoAttendees)]);

        Assert.Contains("SUMMARY:Daily Standup", result);
    }

    [Fact]
    public void Build_SingleMeeting_ContainsRRule()
    {
        var result = IcsFeedBuilder.Build([(Standup, TwoAttendees)]);

        Assert.Contains("RRULE:FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR", result);
    }

    [Fact]
    public void Build_SingleMeeting_ContainsUid()
    {
        var result = IcsFeedBuilder.Build([(Standup, TwoAttendees)]);

        Assert.Contains("UID:pe-standup@teamcalendarsync", result);
    }

    [Fact]
    public void Build_SingleMeeting_ListsAllAttendees()
    {
        var result = IcsFeedBuilder.Build([(Standup, TwoAttendees)]);

        Assert.Contains("ATTENDEE:MAILTO:alice@example.com", result);
        Assert.Contains("ATTENDEE:MAILTO:bob@example.com", result);
    }

    [Fact]
    public void Build_MultipleMeetings_EmitsOneVEventEach()
    {
        var meeting2 = new RecurringMeeting("pd-standup", "product-design", "PD Standup", "FREQ=DAILY", ObserverParticipation.Invited);

        var result = IcsFeedBuilder.Build([
            (Standup, TwoAttendees),
            (meeting2, ["charlie@example.com"]),
        ]);

        Assert.Equal(2, result.Split("BEGIN:VEVENT").Length - 1);
        Assert.Contains("UID:pe-standup@teamcalendarsync", result);
        Assert.Contains("UID:pd-standup@teamcalendarsync", result);
    }

    [Fact]
    public void Build_UsesCrlfLineEndings()
    {
        var result = IcsFeedBuilder.Build([]);

        Assert.Contains("\r\n", result);
    }

    [Fact]
    public void Build_LongSummary_FoldsWithinLimit()
    {
        var longTitle = new string('A', 80);
        var meeting = new RecurringMeeting("m1", "t1", longTitle, "FREQ=DAILY", ObserverParticipation.ReadOnly);

        var result = IcsFeedBuilder.Build([(meeting, [])]);

        foreach (var line in result.Split("\r\n"))
            Assert.True(line.Length <= 75, $"Line exceeds 75 chars ({line.Length}): {line}");
    }

    [Fact]
    public void Build_SeededPlatformEngineering_HasAllDerivedAttendees()
    {
        var members = OrgSeeder.Members.Where(m => m.TeamId == "platform-engineering").ToList();
        var observers = OrgSeeder.Observers.Where(o => o.TeamId == "platform-engineering").ToList();
        var meetings = OrgSeeder.Meetings.Where(m => m.TeamId == "platform-engineering").ToList();

        var entries = meetings.Select(m =>
            (m, AttendancePolicy.DeriveAttendees(members, observers, m.ObserverParticipation)));

        var result = IcsFeedBuilder.Build(entries);

        Assert.Contains("ATTENDEE:MAILTO:alice@example.com", result);
        Assert.Contains("ATTENDEE:MAILTO:bob@example.com", result);
        Assert.Contains("ATTENDEE:MAILTO:carol@example.com", result);
    }

    [Fact]
    public void Build_StandupMeeting_ReadOnly_ExcludesObservers()
    {
        var members = OrgSeeder.Members.Where(m => m.TeamId == "platform-engineering").ToList();
        var observers = OrgSeeder.Observers.Where(o => o.TeamId == "platform-engineering").ToList();
        var standup = OrgSeeder.Meetings.Single(m => m.Id == "pe-standup");

        var attendees = AttendancePolicy.DeriveAttendees(members, observers, standup.ObserverParticipation);
        var result = IcsFeedBuilder.Build([(standup, attendees)]);

        // grace is observer-only; should not appear in ReadOnly standup
        Assert.DoesNotContain("ATTENDEE:MAILTO:grace@example.com", result);
    }

    [Fact]
    public void Build_SprintPlanning_Invited_IncludesObservers()
    {
        var members = OrgSeeder.Members.Where(m => m.TeamId == "platform-engineering").ToList();
        var observers = OrgSeeder.Observers.Where(o => o.TeamId == "platform-engineering").ToList();
        var planning = OrgSeeder.Meetings.Single(m => m.Id == "pe-sprint-planning");

        var attendees = AttendancePolicy.DeriveAttendees(members, observers, planning.ObserverParticipation);
        var result = IcsFeedBuilder.Build([(planning, attendees)]);

        // grace is an observer and this meeting is Invited
        Assert.Contains("ATTENDEE:MAILTO:grace@example.com", result);
    }
}
