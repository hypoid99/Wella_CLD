using Wella.Models;

namespace Wella.Services;

public static class FileService
{
    private static readonly string DataDir =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

    static FileService() => Directory.CreateDirectory(DataDir);

    public static string GetPath(string fileName) => Path.Combine(DataDir, fileName);

    // ── 공통 ──────────────────────────────────────────────────────────────
    public static IEnumerable<string> ReadLines(string fileName)
    {
        var path = GetPath(fileName);
        if (!File.Exists(path)) return [];
        return File.ReadAllLines(path)
                   .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#'));
    }

    public static void WriteLines(string fileName, IEnumerable<string> lines) =>
        File.WriteAllLines(GetPath(fileName), lines);

    public static string ReadText(string fileName)
    {
        var path = GetPath(fileName);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    public static void WriteText(string fileName, string content) =>
        File.WriteAllText(GetPath(fileName), content);

    // ── 달력 ──────────────────────────────────────────────────────────────
    private const string CalendarFile = "calendar.txt";

    public static List<CalendarEvent> LoadCalendarEvents()
    {
        var list = new List<CalendarEvent>();
        foreach (var line in ReadLines(CalendarFile))
        {
            var ev = CalendarEvent.FromLine(line);
            if (ev != null) list.Add(ev);
        }
        return list;
    }

    public static void SaveCalendarEvents(IEnumerable<CalendarEvent> events)
    {
        var lines = new List<string> { "# Wella Calendar Data" };
        lines.AddRange(events.Select(e => e.ToLine()));
        WriteLines(CalendarFile, lines);
    }
}
