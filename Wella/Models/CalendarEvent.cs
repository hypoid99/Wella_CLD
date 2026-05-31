namespace Wella.Models;

public class CalendarEvent
{
    public int      Id          { get; set; }
    public DateTime Date        { get; set; }
    public string   Title       { get; set; } = "";
    public string   Description { get; set; } = "";
    public string   StartTime   { get; set; } = "";
    public string   EndTime     { get; set; } = "";

    // 파일 포맷: ID|YYYY-MM-DD|TITLE|DESC|START|END
    public string ToLine() =>
        $"{Id}|{Date:yyyy-MM-dd}|{Escape(Title)}|{Escape(Description)}|{StartTime}|{EndTime}";

    public static CalendarEvent? FromLine(string line)
    {
        var p = line.Split('|');
        if (p.Length < 6) return null;
        if (!DateTime.TryParse(p[1], out var date)) return null;
        return new CalendarEvent
        {
            Id          = int.TryParse(p[0], out var id) ? id : 0,
            Date        = date,
            Title       = Unescape(p[2]),
            Description = Unescape(p[3]),
            StartTime   = p[4],
            EndTime     = p[5]
        };
    }

    private static string Escape  (string s) => s.Replace("|", "\\|").Replace("\n", "\\n");
    private static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\|", "|");
}
