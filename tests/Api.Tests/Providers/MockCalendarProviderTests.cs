using Api.Providers;
using Xunit;

namespace Api.Tests.Providers;

public class MockCalendarProviderTests
{
    private readonly MockCalendarProvider _provider = new();

    [Fact]
    public async Task GetAttendees_ForUnknownMeeting_ReturnsEmpty()
    {
        var result = await _provider.GetAttendeesAsync("meeting-1");
        Assert.Empty(result);
    }

    [Fact]
    public async Task SetThenGetAttendees_ReturnsStoredAttendees()
    {
        await _provider.SetAttendeesAsync("meeting-1", ["alice@example.com", "bob@example.com"]);
        var result = await _provider.GetAttendeesAsync("meeting-1");
        Assert.Equal(["alice@example.com", "bob@example.com"], result);
    }

    [Fact]
    public async Task SetAttendees_ReplacesExistingAttendees()
    {
        await _provider.SetAttendeesAsync("meeting-1", ["alice@example.com", "bob@example.com"]);
        await _provider.SetAttendeesAsync("meeting-1", ["charlie@example.com"]);
        var result = await _provider.GetAttendeesAsync("meeting-1");
        Assert.Equal(["charlie@example.com"], result);
    }

    [Fact]
    public async Task SetAttendees_DuplicateEmailsDifferentCase_StoredOnce()
    {
        await _provider.SetAttendeesAsync("meeting-1", ["alice@example.com", "ALICE@example.com"]);
        var result = await _provider.GetAttendeesAsync("meeting-1");
        Assert.Single(result);
    }

    [Fact]
    public async Task GetAttendees_ReturnsAlphabeticalOrder()
    {
        await _provider.SetAttendeesAsync("meeting-1", ["charlie@example.com", "alice@example.com", "bob@example.com"]);
        var result = await _provider.GetAttendeesAsync("meeting-1");
        Assert.Equal(["alice@example.com", "bob@example.com", "charlie@example.com"], result);
    }

    [Fact]
    public async Task Meetings_AreIsolatedFromEachOther()
    {
        await _provider.SetAttendeesAsync("meeting-1", ["alice@example.com"]);
        await _provider.SetAttendeesAsync("meeting-2", ["bob@example.com"]);
        Assert.Equal(["alice@example.com"], await _provider.GetAttendeesAsync("meeting-1"));
        Assert.Equal(["bob@example.com"], await _provider.GetAttendeesAsync("meeting-2"));
    }
}
