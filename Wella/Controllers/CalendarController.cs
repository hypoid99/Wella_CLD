using Wella.Models;
using Wella.Services;

namespace Wella.Controllers;

public class CalendarController
{
    private List<CalendarEvent> _events;
    private int _nextId;

    public IReadOnlyList<CalendarEvent> Events => _events;

    public CalendarController()
    {
        _events = FileService.LoadCalendarEvents();
        _nextId = _events.Count > 0 ? _events.Max(e => e.Id) + 1 : 1;
    }

    public CalendarEvent Add(DateTime date, string title, string desc,
                             string start, string end)
    {
        var ev = new CalendarEvent
        {
            Id          = _nextId++,
            Date        = date.Date,
            Title       = title,
            Description = desc,
            StartTime   = start,
            EndTime     = end
        };
        _events.Add(ev);
        Save();
        return ev;
    }

    public void Update(int id, string title, string desc, string start, string end)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        if (ev == null) return;
        ev.Title       = title;
        ev.Description = desc;
        ev.StartTime   = start;
        ev.EndTime     = end;
        Save();
    }

    public void Delete(int id)
    {
        _events.RemoveAll(e => e.Id == id);
        Save();
    }

    public List<CalendarEvent> GetByDate(DateTime date) =>
        _events.Where(e => e.Date.Date == date.Date)
               .OrderBy(e => e.StartTime)
               .ToList();

    public List<CalendarEvent> GetByMonth(int year, int month) =>
        _events.Where(e => e.Date.Year == year && e.Date.Month == month).ToList();

    private void Save() => FileService.SaveCalendarEvents(_events);
}
