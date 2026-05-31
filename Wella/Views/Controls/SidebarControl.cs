using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Wella.Controllers;

namespace Wella.Views.Controls;

public class SidebarControl : Panel
{
    public event EventHandler<ToolType>? ToolClicked;

    private ToolType  _activeTool = ToolType.Calendar;
    private ToolType? _hoverTool;

    // 이모지 대신 안전한 유니코드 기호 사용
    private static readonly (ToolType Tool, string Icon, string Label)[] Items =
    {
        (ToolType.Calendar,   "▦", "달력"),
        (ToolType.Todo,       "✔", "할일"),
        (ToolType.Memo,       "▤", "메모"),
        (ToolType.Calculator, "#", "계산기"),
    };

    private readonly Dictionary<ToolType, Rectangle> _itemRects = new();

    // ── 색상 ─────────────────────────────────────────────────────────────
    private static readonly Color BgColor     = Color.FromArgb(52,  73,  94);
    private static readonly Color AccentColor = Color.FromArgb(26, 188, 156);
    private static readonly Color BorderColor = Color.FromArgb(44,  62,  80);
    private static readonly Color TextDim     = Color.FromArgb(148, 180, 200);

    private const int ItemH     = 52;
    private const int TopOffset = 40;
    private const int PadH      = 8;

    // ── 캐시된 GDI 객체 (OnPaint 에서 new 금지) ─────────────────────────
    private readonly Font       _sectionFont   = new("Segoe UI",  8f,  FontStyle.Regular);
    private readonly Font       _iconFont      = new("Segoe UI", 14f,  FontStyle.Regular);
    private readonly Font       _labelFont     = new("Segoe UI", 10f,  FontStyle.Regular);
    private readonly Font       _labelBoldFont = new("Segoe UI", 10f,  FontStyle.Bold);
    private readonly SolidBrush _sectionBrush  = new(Color.FromArgb(100, 135, 160));
    private readonly SolidBrush _accentBrush   = new(AccentColor);
    private readonly SolidBrush _dimBrush      = new(TextDim);
    private readonly SolidBrush _whiteBrush    = new(Color.White);
    private readonly SolidBrush _accentBgBrush = new(Color.FromArgb(35, 26, 188, 156));
    private readonly SolidBrush _hoverBgBrush  = new(Color.FromArgb(40, 255, 255, 255));
    private readonly Pen        _borderPen     = new(BorderColor, 1);

    public SidebarControl()
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

        g.DrawString("도 구", _sectionFont, _sectionBrush, new PointF(16f, 16f));

        _itemRects.Clear();
        for (int i = 0; i < Items.Length; i++)
        {
            var (tool, icon, label) = Items[i];
            int y    = TopOffset + i * ItemH;
            var rect = new Rectangle(PadH, y, Width - PadH * 2, ItemH - 2);
            _itemRects[tool] = rect;
            DrawItem(g, tool, icon, label, rect);
        }

        g.DrawLine(_borderPen, Width - 1, 0, Width - 1, Height);
    }

    private void DrawItem(Graphics g, ToolType tool, string icon, string label, Rectangle rect)
    {
        bool active = _activeTool == tool;
        bool hover  = _hoverTool  == tool;

        if (active)
        {
            // 좌측 액센트 바
            g.FillRectangle(_accentBrush, new Rectangle(rect.X, rect.Y, 4, rect.Height));

            // 배경
            using var bgPath = RoundRect(new Rectangle(rect.X + 4, rect.Y, rect.Width - 4, rect.Height), 6);
            g.FillPath(_accentBgBrush, bgPath);
        }
        else if (hover)
        {
            using var hPath = RoundRect(rect, 6);
            g.FillPath(_hoverBgBrush, hPath);
        }

        // 아이콘
        var iconBrush = active ? _accentBrush : _dimBrush;
        g.DrawString(icon, _iconFont, iconBrush, new PointF(rect.X + 14f, rect.Y + 12f));

        // 라벨
        var lblBrush = active ? _whiteBrush : _dimBrush;
        var lblFont  = active ? _labelBoldFont : _labelFont;
        g.DrawString(label, lblFont, lblBrush, new PointF(rect.X + 48f, rect.Y + 16f));
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
        foreach (var kv in _itemRects)
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
            _sectionFont.Dispose();   _iconFont.Dispose();
            _labelFont.Dispose();     _labelBoldFont.Dispose();
            _sectionBrush.Dispose();  _accentBrush.Dispose();
            _dimBrush.Dispose();      _whiteBrush.Dispose();
            _accentBgBrush.Dispose(); _hoverBgBrush.Dispose();
            _borderPen.Dispose();
        }
        base.Dispose(disposing);
    }
}
