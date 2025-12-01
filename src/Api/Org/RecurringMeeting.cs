namespace Api.Org;

public sealed record RecurringMeeting(string Id, string TeamId, string Title, string Recurrence, ObserverParticipation ObserverParticipation);
