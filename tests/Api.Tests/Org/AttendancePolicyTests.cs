using Api.Org;
using Xunit;

namespace Api.Tests.Org;

public class AttendancePolicyTests
{
    [Fact]
    public void DeriveAttendees_MembersOnly_ReturnsMemberEmails()
    {
        var members = new[] { new TeamMember("t1", "alice@example.com", "Alice", "Engineer") };

        var result = AttendancePolicy.DeriveAttendees(members, [], ObserverParticipation.ReadOnly);

        Assert.Equal(["alice@example.com"], result);
    }

    [Fact]
    public void DeriveAttendees_ObserversInvited_IncludesObserverEmails()
    {
        var members = new[] { new TeamMember("t1", "alice@example.com", "Alice", "Engineer") };
        var observers = new[] { new TeamObserver("t1", "bob@example.com", "Bob") };

        var result = AttendancePolicy.DeriveAttendees(members, observers, ObserverParticipation.Invited);

        Assert.Equal(["alice@example.com", "bob@example.com"], result);
    }

    [Fact]
    public void DeriveAttendees_ObserversReadOnly_ExcludesObserverEmails()
    {
        var members = new[] { new TeamMember("t1", "alice@example.com", "Alice", "Engineer") };
        var observers = new[] { new TeamObserver("t1", "bob@example.com", "Bob") };

        var result = AttendancePolicy.DeriveAttendees(members, observers, ObserverParticipation.ReadOnly);

        Assert.Equal(["alice@example.com"], result);
    }

    [Fact]
    public void DeriveAttendees_MemberAlsoObserver_IsDeduplicatedInOutput()
    {
        var members = new[] { new TeamMember("t1", "alice@example.com", "Alice", "Engineer") };
        var observers = new[] { new TeamObserver("t1", "alice@example.com", "Alice") };

        var result = AttendancePolicy.DeriveAttendees(members, observers, ObserverParticipation.Invited);

        Assert.Single(result);
        Assert.Equal("alice@example.com", result[0]);
    }

    [Fact]
    public void DeriveAttendees_EmptyMembers_ObserversInvited_ReturnsObserverEmails()
    {
        var observers = new[] { new TeamObserver("t1", "bob@example.com", "Bob") };

        var result = AttendancePolicy.DeriveAttendees([], observers, ObserverParticipation.Invited);

        Assert.Equal(["bob@example.com"], result);
    }

    [Fact]
    public void DeriveAttendees_EmailComparisonIsCaseInsensitive()
    {
        var members = new[] { new TeamMember("t1", "alice@example.com", "Alice", "Engineer") };
        var observers = new[] { new TeamObserver("t1", "ALICE@EXAMPLE.COM", "Alice") };

        var result = AttendancePolicy.DeriveAttendees(members, observers, ObserverParticipation.Invited);

        Assert.Single(result);
    }

    [Fact]
    public void DeriveAttendees_OutputIsSortedAlphabetically()
    {
        var members = new[]
        {
            new TeamMember("t1", "charlie@example.com", "Charlie", "Engineer"),
            new TeamMember("t1", "alice@example.com", "Alice", "Engineer"),
        };
        var observers = new[] { new TeamObserver("t1", "bob@example.com", "Bob") };

        var result = AttendancePolicy.DeriveAttendees(members, observers, ObserverParticipation.Invited);

        Assert.Equal(["alice@example.com", "bob@example.com", "charlie@example.com"], result);
    }

    [Fact]
    public void DeriveAttendees_SeedPlatformEngineering_Invited_ReturnsExpectedSet()
    {
        var members = OrgSeeder.Members.Where(m => m.TeamId == "platform-engineering").ToList();
        var observers = OrgSeeder.Observers.Where(o => o.TeamId == "platform-engineering").ToList();

        var result = AttendancePolicy.DeriveAttendees(members, observers, ObserverParticipation.Invited);

        Assert.Equal(
            ["alice@example.com", "bob@example.com", "carol@example.com", "dan@example.com", "grace@example.com"],
            result);
    }

    [Fact]
    public void DeriveAttendees_SeedPlatformEngineering_ReadOnly_ReturnsMembersOnly()
    {
        var members = OrgSeeder.Members.Where(m => m.TeamId == "platform-engineering").ToList();
        var observers = OrgSeeder.Observers.Where(o => o.TeamId == "platform-engineering").ToList();

        var result = AttendancePolicy.DeriveAttendees(members, observers, ObserverParticipation.ReadOnly);

        Assert.Equal(["alice@example.com", "bob@example.com", "carol@example.com"], result);
    }

    [Fact]
    public void DeriveAttendees_SeedProductDesign_Invited_ReturnsExpectedSet()
    {
        var members = OrgSeeder.Members.Where(m => m.TeamId == "product-design").ToList();
        var observers = OrgSeeder.Observers.Where(o => o.TeamId == "product-design").ToList();

        var result = AttendancePolicy.DeriveAttendees(members, observers, ObserverParticipation.Invited);

        Assert.Equal(
            ["alice@example.com", "dan@example.com", "eve@example.com", "frank@example.com", "grace@example.com"],
            result);
    }

    [Fact]
    public void DeriveAttendees_SeedProductDesign_ReadOnly_ReturnsMembersOnly()
    {
        var members = OrgSeeder.Members.Where(m => m.TeamId == "product-design").ToList();
        var observers = OrgSeeder.Observers.Where(o => o.TeamId == "product-design").ToList();

        var result = AttendancePolicy.DeriveAttendees(members, observers, ObserverParticipation.ReadOnly);

        Assert.Equal(["dan@example.com", "eve@example.com", "frank@example.com"], result);
    }
}
