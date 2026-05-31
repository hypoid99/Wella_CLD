using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Wella.Controllers;

namespace Wella.Views.Controls;

public class TopMenuBar : Panel
{
    public event EventHandler<ToolType>? ToolClicked;

    private ToolType  _activeTool = ToolType.Calendar;
    private ToolType? _hoverTool;

    // 이모지 대신 안전한 유니코드 기호 사용
    private static readonly (ToolType Tool, string Icon, string Label)[] Tools =
    {
        (ToolType.Calendar,   "▦", "달력"),
        (ToolType.Todo,       "✔", "할일"),
        (ToolType.Memo,       "▤", "메모"),
        (ToolType.Calculator, "#", "계산기"),
    };

    private readonly Dictionary<ToolType, Rectangle> _btnRects = new();

    // ── 색상 ─────────────────────────────────────────────────────────────
    private static readonly Color BgColor     = Color.FromArgb(44,  62,  80);
    private static readonly Color AccentColor = Color.FromArgb(26, 188, 156);
    private static readonly Color HoverColor  = Color.FromArgb(60,  80, 100);
    private static readonly Color TextDim     = Color.FromArgb(160, 190, 210);

    private const int BtnWidth  = 96;
    private const int BtnMargin = 4;

    // ── 캐시된 GDI 객체 (OnPaint 에서 new 금지) ─────────────────────────
    private readonly Font       _boldFont   = new("Segoe UI", 16f, FontStyle.Bold);
    private readonly Font       _regFont    = new("Segoe UI", 14f, FontStyle.Regular);
    private readonly Font       _iconFont   = new("Segoe UI", 13f, FontStyle.Regular);
    private readonly Font       _labelFont  = new("Segoe UI",  8.5f);
    private readonly SolidBrush _accBrush   = new(AccentColor);
    private readonly SolidBrush _whiteBrush = new(Color.White);
    private readonly SolidBrush _dimBrush   = new(TextDim);
    private readonly SolidBrush _activeBg   = new(AccentColor);
    private readonly SolidBrush _hoverBg    = new(HoverColor);
    private readonly Pen        _borderPen  = new(Color.FromArgb(36, 52, 68), 1);

    public TopMenuBar()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint  |
                 ControlStyles.UserPaint, true);
        BackColor = BgColor;
    }

    public void SetActiveTool(ToolType tool) { _activeTool = tool; Invalidate(); }

    // ── Paint ────────────────────────────────────────────────────────────
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(BgColor);

        DrawLogo(g);
        DrawButtons(g);
        g.DrawLine(_borderPen, 0, Height - 1, Width, Height - 1);
    }

    private void DrawLogo(Graphics g)
    {
        g.DrawString("W",    _boldFont, _accBrush,   new PointF(16f, 13f));
        g.DrawString("ella", _regFont,  _whiteBrush, new PointF(31f, 15f));
    }

    private void DrawButtons(Graphics g)
    {
        int startX = Width - Tools.Length * BtnWidth - 12;
        _btnRects.Clear();

        for (int i = 0; i < Tools.Length; i++)
        {
            var (tool, icon, label) = Tools[i];
            var rect = new Rectangle(startX + i * BtnWidth + BtnMargin,
                                     BtnMargin,
                                     BtnWidth - BtnMargin * 2,
                                     Height   - BtnMargin * 2);
            _btnRects[tool] = rect;

            bool active = _activeTool == tool;
            bool hover  = _hoverTool  == tool;

            // 배경
            if (active || hover)
            {
                using var path = RoundRect(rect, 6);
                g.FillPath(active ? _activeBg : _hoverBg, path);
            }

            // 아이콘
            var fg = active ? _whiteBrush : _dimBrush;
            var iconSz = g.MeasureString(icon, _iconFont);
            g.DrawString(icon, _iconFont, fg,
                new PointF(rect.X + (rect.Width - iconSz.Width) / 2f, rect.Y + 5f));

            // 라벨
            var lblSz = g.MeasureString(label, _labelFont);
            g.DrawString(label, _labelFont, fg,
                new PointF(rect.X + (rect.Width - lblSz.Width) / 2f, rect.Y + 28f));
        }
    }

    // ── Mouse ────────────────────────────────────────────────────────────
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hit = HitTest(e.Location);
        if (_hoverTool != hit)
        {
            _hoverTool = hit;
            Cursor = hit.HasValue ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverTool = null;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var hit = HitTest(e.Location);
        if (hit.HasValue) ToolClicked?.Invoke(this, hit.Value);
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private ToolType? HitTest(Point p)
    {
        foreach (var kv in _btnRects)
            if (kv.Value.Contains(p)) return kv.Key;
        return null;
    }

    private static GraphicsPath RoundRect(Rectangle r, int radius)
    {
        int d    = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X,         r.Y,          d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
        path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    // ── Dispose ──────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _boldFont.Dispose();   _regFont.Dispose();
            _iconFont.Dispose();   _labelFont.Dispose();
            _accBrush.Dispose();   _whiteBrush.Dispose();
            _dimBrush.Dispose();   _activeBg.Dispose();
            _hoverBg.Dispose();    _borderPen.Dispose();
        }
        base.Dispose(disposing);
    }
}
