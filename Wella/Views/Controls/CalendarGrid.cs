using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Wella.Models;

namespace Wella.Views.Controls;

public class CalendarGrid : Control
{
    public event EventHandler<DateTime>? DateSelected;

    private DateTime  _month;
    private DateTime? _selected;
    private List<CalendarEvent> _events = [];

    // 셀 위치 캐시 (OnPaint 마다 재계산)
    private readonly Rectangle[,] _rects = new Rectangle[6, 7];
    private readonly DateTime[,]  _dates = new DateTime[6, 7];

    // 열 X 위치 (나머지 픽셀까지 정확히 분배)
    private readonly int[] _colX = new int[8];

    // ── 색상 ─────────────────────────────────────────────────────────────
    private static readonly Color BgColor    = Color.FromArgb(245, 246, 250);
    private static readonly Color HeaderBg   = Color.FromArgb(228, 232, 238);
    private static readonly Color CellBorder = Color.FromArgb(218, 223, 230);
    private static readonly Color TodayBg    = Color.FromArgb(26, 188, 156);
    private static readonly Color SelBorder  = Color.FromArgb(52, 152, 219);
    private static readonly Color SelBg      = Color.FromArgb(25, 52, 152, 219);
    private static readonly Color SunColor   = Color.FromArgb(210, 70, 70);
    private static readonly Color SatColor   = Color.FromArgb(60, 110, 220);
    private static readonly Color DimColor   = Color.FromArgb(185, 198, 210);
    private static readonly Color NormalClr  = Color.FromArgb(52,  73,  94);
    private static readonly Color DotColor   = Color.FromArgb(26, 188, 156);

    // ── GDI 캐시 ─────────────────────────────────────────────────────────
    private readonly Font _headFont  = new("Segoe UI",  9f, FontStyle.Bold);
    private readonly Font _dayFont   = new("Segoe UI", 10f, FontStyle.Regular);
    private readonly Font _todayFont = new("Segoe UI", 10f, FontStyle.Bold);
    private readonly Pen  _cellPen   = new(CellBorder, 1);

    private const int HeaderH = 32;

    public CalendarGrid()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint  |
                 ControlStyles.UserPaint, true);
        ResizeRedraw = true;   // 리사이즈 시 자동 재드로우
        BackColor    = BgColor;
        _month    = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _selected = DateTime.Today;
    }

    // ── 공개 API ─────────────────────────────────────────────────────────
    public DateTime CurrentMonth => _month;

    public void SetMonth(int year, int month)
    {
        _month = new DateTime(year, month, 1);
        Invalidate();
    }

    public void SetEvents(List<CalendarEvent> events) { _events = events; Invalidate(); }
    public void SelectDate(DateTime d) { _selected = d; Invalidate(); }

    // ── 리사이즈 시 강제 재드로우 ────────────────────────────────────────
    protected override void OnResize(EventArgs e) { base.OnResize(e); Invalidate(); }

    // ── 열 X 좌표 계산 (나머지 픽셀 분배) ───────────────────────────────
    private void RecalcColumns()
    {
        if (Width <= 0) return;
        int cw  = Width / 7;
        int rem = Width % 7;
        _colX[0] = 0;
        for (int i = 1; i <= 7; i++)
            _colX[i] = _colX[i - 1] + cw + (i - 1 < rem ? 1 : 0);
    }

    // ── Paint ────────────────────────────────────────────────────────────
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 0 || Height <= 0) return;

        RecalcColumns();

        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(BgColor);

        DrawHeader(g);
        DrawCells(g);
    }

    private void DrawHeader(Graphics g)
    {
        // 헤더 배경
        using var bg = new SolidBrush(HeaderBg);
        g.FillRectangle(bg, 0, 0, Width, HeaderH);

        string[] days = { "일", "월", "화", "수", "목", "금", "토" };

        for (int i = 0; i < 7; i++)
        {
            int x  = _colX[i];
            int cw = _colX[i + 1] - _colX[i];
            var rc = new Rectangle(x, 0, cw, HeaderH);

            var color = i == 0 ? SunColor : i == 6 ? SatColor : Color.FromArgb(70, 90, 115);
            using var br = new SolidBrush(color);
            var sz = g.MeasureString(days[i], _headFont);
            g.DrawString(days[i], _headFont, br,
                rc.X + (rc.Width  - sz.Width)  / 2f,
                rc.Y + (rc.Height - sz.Height) / 2f);

            // 열 구분선
            if (i < 6)
                g.DrawLine(_cellPen, _colX[i + 1], 0, _colX[i + 1], HeaderH);
        }

        // 헤더 하단 구분선
        using var boldPen = new Pen(Color.FromArgb(180, 190, 200), 1);
        g.DrawLine(boldPen, 0, HeaderH, Width, HeaderH);
    }

    private void DrawCells(Graphics g)
    {
        int gridH  = Height - HeaderH;
        int ch     = gridH / 6;
        int offset = (int)_month.DayOfWeek;
        var start  = _month.AddDays(-offset);

        for (int row = 0; row < 6; row++)
        for (int col = 0; col < 7; col++)
        {
            int x  = _colX[col];
            int cw = _colX[col + 1] - _colX[col];
            var rect = new Rectangle(x, HeaderH + row * ch, cw, ch);
            var date = start.AddDays(row * 7 + col);
            _rects[row, col] = rect;
            _dates[row, col] = date;
            DrawCell(g, date, rect, col);
        }
    }

    private void DrawCell(Graphics g, DateTime date, Rectangle r, int col)
    {
        bool isToday    = date.Date == DateTime.Today;
        bool isSel      = _selected?.Date == date.Date;
        bool isCurMonth = date.Month == _month.Month;

        // 선택 배경
        if (isSel && !isToday)
        {
            using var sb = new SolidBrush(SelBg);
            g.FillRectangle(sb, r);
        }

        // 셀 테두리
        g.DrawRectangle(_cellPen, r);

        // 선택 테두리
        if (isSel)
        {
            using var sp = new Pen(SelBorder, 2);
            g.DrawRectangle(sp, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
        }

        // 날짜 숫자
        string txt = date.Day.ToString();
        Color fg = !isCurMonth ? DimColor
                  : col == 0  ? SunColor
                  : col == 6  ? SatColor
                  : NormalClr;

        if (isToday)
        {
            int   sz  = 26;
            float cx  = r.X + sz / 2f + 6;
            float cy  = r.Y + 5;
            using var tBg = new SolidBrush(TodayBg);
            g.FillEllipse(tBg, cx - sz / 2f, cy, sz, sz);
            var tsz = g.MeasureString(txt, _todayFont);
            using var tw = new SolidBrush(Color.White);
            g.DrawString(txt, _todayFont, tw,
                cx - tsz.Width / 2f + 1,
                cy + (sz - tsz.Height) / 2f);
        }
        else
        {
            using var fb = new SolidBrush(fg);
            g.DrawString(txt, _dayFont, fb, new PointF(r.X + 6f, r.Y + 6f));
        }

        // 이벤트 점
        var evList = _events.Where(e => e.Date.Date == date.Date).ToList();
        if (evList.Count > 0)
        {
            int dots   = Math.Min(evList.Count, 3);
            int dotSz  = 5;
            int gap    = 3;
            int totalW = dots * dotSz + (dots - 1) * gap;
            int sx     = r.X + (r.Width - totalW) / 2;
            int dy     = r.Bottom - dotSz - 5;
            using var db = new SolidBrush(DotColor);
            for (int d = 0; d < dots; d++)
                g.FillEllipse(db, sx + d * (dotSz + gap), dy, dotSz, dotSz);
        }
    }

    // ── Mouse ────────────────────────────────────────────────────────────
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        for (int row = 0; row < 6; row++)
        for (int col = 0; col < 7; col++)
        {
            if (_rects[row, col].Contains(e.Location))
            {
                _selected = _dates[row, col];
                Invalidate();
                DateSelected?.Invoke(this, _selected.Value);
                return;
            }
        }
    }

    // ── Dispose ──────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _headFont.Dispose(); _dayFont.Dispose();
            _todayFont.Dispose(); _cellPen.Dispose();
        }
        base.Dispose(disposing);
    }
}
