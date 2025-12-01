using System.Text;
using Api.Org;

namespace Api.Feed;

public static class IcsFeedBuilder
{
    private const string DtStart = "20240101T090000Z";

    public static string Build(IEnumerable<(RecurringMeeting Meeting, IReadOnlyList<string> Attendees)> entries)
    {
        var sb = new StringBuilder();

        Fold(sb, "BEGIN:VCALENDAR");
        Fold(sb, "VERSION:2.0");
        Fold(sb, "PRODID:-//Team Calendar Sync//EN");
        Fold(sb, "CALSCALE:GREGORIAN");
        Fold(sb, "X-WR-CALNAME:Team Calendar Sync");

        foreach (var (meeting, attendees) in entries)
        {
            Fold(sb, "BEGIN:VEVENT");
            Fold(sb, $"UID:{meeting.Id}@teamcalendarsync");
            Fold(sb, $"SUMMARY:{EscapeText(meeting.Title)}");
            Fold(sb, $"DTSTART:{DtStart}");
            Fold(sb, "DURATION:PT1H");
            Fold(sb, $"RRULE:{meeting.Recurrence}");
            Fold(sb, "STATUS:CONFIRMED");
            foreach (var attendee in attendees)
                Fold(sb, $"ATTENDEE:MAILTO:{attendee}");
            Fold(sb, "END:VEVENT");
        }

        Fold(sb, "END:VCALENDAR");

        return sb.ToString();
    }

    // RFC 5545 §3.1: lines longer than 75 octets MUST be folded with CRLF + single space.
    private static void Fold(StringBuilder sb, string line)
    {
        const int max = 75;
        if (line.Length <= max)
        {
            sb.Append(line).Append("\r\n");
            return;
        }

        sb.Append(line[..max]).Append("\r\n");
        var rest = line[max..];
        while (rest.Length > 0)
        {
            var take = Math.Min(rest.Length, max - 1);
            sb.Append(' ').Append(rest[..take]).Append("\r\n");
            rest = rest[take..];
        }
    }

    private static string EscapeText(string text) =>
        text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
}
