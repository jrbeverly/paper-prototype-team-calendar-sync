namespace Api.Org;

public static class AttendancePolicy
{
    public static IReadOnlyList<string> DeriveAttendees(
        IEnumerable<TeamMember> members,
        IEnumerable<TeamObserver> observers,
        ObserverParticipation observerParticipation)
    {
        var attendees = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in members)
            attendees.Add(member.Email);

        if (observerParticipation == ObserverParticipation.Invited)
            foreach (var observer in observers)
                attendees.Add(observer.Email);

        return [.. attendees.Order(StringComparer.OrdinalIgnoreCase)];
    }
}
