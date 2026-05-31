using Wella.Controllers;
using Wella.Models;
using Wella.Views.Controls;

namespace Wella.Views.Panels;

public class CalendarPanel : UserControl
{
    private readonly CalendarController _ctrl;
    private CalendarGrid _grid    = null!;
    private Panel        _navBar  = null!;
    private Panel        _side    = null!;
    private DateTime     _selDate = DateTime.Today;

    private Label   _monthLbl   = null!;
    private Label   _selDateLbl = null!;
    private ListBox _eventList  = null!;

    private const int NavH   = 48;
    private const int SideW  = 260;

    private static readonly Color NavBg    = Color.FromArgb(44,  62,  80);
    private static readonly Color SideBg   = Color.FromArgb(255, 255, 255);
    private static readonly Color AccentCl = Color.FromArgb(26,  188, 156);
    private static readonly Color DeleteCl = Color.FromArgb(192,  57,  43);
    private static readonly Color DivColor = Color.FromArgb(218, 224, 232);

    public CalendarPanel()
    {
        _ctrl = new CalendarController();
        BackColor = Color.FromArgb(245, 246, 250);
        BuildControls();
        RefreshGrid();
        RefreshEventList();
    }

    // ── 컨트롤 생성 (위치/크기는 ManualLayout에서 지정) ─────────────────
    private void BuildControls()
    {
        // ① 달력 그리드 (가장 먼저 생성 — 다른 컨트롤이 참조)
        _grid = new CalendarGrid();
        _grid.DateSelected += OnDateSelected;

        // ② 상단 네비바
        _navBar = BuildNavBar();

        // ③ 우측 이벤트 패널
        _side = BuildSidePanel();

        // 모두 Dock 없이 추가 (ManualLayout이 위치/크기를 직접 지정)
        Controls.Add(_grid);
        Controls.Add(_side);
        Controls.Add(_navBar);   // 맨 마지막 = 최상위 z-order (날짜 셀보다 앞)
    }

    // ── 수동 배치 — 폼 리사이즈마다 호출 ────────────────────────────────
    private void ManualLayout()
    {
        if (_grid == null) return;
        int w = ClientSize.Width;
        int h = ClientSize.Height;

        _navBar.SetBounds(0, 0, w, NavH);
        _side  .SetBounds(w - SideW, NavH, SideW, h - NavH);
        _grid  .SetBounds(0, NavH, w - SideW, h - NavH);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ManualLayout();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ManualLayout();
        UpdateMonthLabel();
    }

    // ── 네비바 ───────────────────────────────────────────────────────────
    private Panel BuildNavBar()
    {
        var nav = new Panel { BackColor = NavBg };

        var prevBtn = NavBtn("<", 8);
        var nextBtn = NavBtn(">", 56);
        prevBtn.Click += (_, _) => ShiftMonth(-1);
        nextBtn.Click += (_, _) => ShiftMonth(+1);

        _monthLbl = new Label
        {
            Bounds    = new Rectangle(108, 0, 220, NavH),
            Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        nav.Controls.AddRange([prevBtn, nextBtn, _monthLbl]);
        return nav;
    }

    private static Button NavBtn(string t, int x) => new()
    {
        Text      = t,
        Bounds    = new Rectangle(x, 7, 40, 34),
        FlatStyle = FlatStyle.Flat,
        Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
        ForeColor = Color.White,
        BackColor = Color.FromArgb(62, 82, 104),
        Cursor    = Cursors.Hand,
        FlatAppearance = { BorderSize = 0 }
    };

    // ── 우측 이벤트 패널 ─────────────────────────────────────────────────
    private Panel BuildSidePanel()
    {
        var side = new Panel { BackColor = SideBg };

        // 좌측 구분선 (1px)
        var border = new Panel
        {
            Bounds    = new Rectangle(0, 0, 1, 9999),
            BackColor = DivColor,
            Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
        };

        _selDateLbl = new Label
        {
            Bounds    = new Rectangle(1, 0, SideW - 1, 44),
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
            BackColor = Color.FromArgb(248, 249, 251),
            Text      = _selDate.ToString("yyyy년 M월 d일 (ddd)")
        };

        var divLine = new Panel
        {
            Bounds    = new Rectangle(1, 44, SideW - 1, 1),
            BackColor = DivColor
        };

        _eventList = new ListBox
        {
            Bounds         = new Rectangle(1, 45, SideW - 1, 9999),
            Font           = new Font("Segoe UI", 9.5f),
            BorderStyle    = BorderStyle.None,
            BackColor      = SideBg,
            ItemHeight     = 32,
            IntegralHeight = false,
            DrawMode       = DrawMode.OwnerDrawFixed,
            Anchor         = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _eventList.DrawItem += DrawEventItem;

        var btnBar = new Panel
        {
            BackColor = SideBg,
            Bounds    = new Rectangle(1, 9999, SideW - 1, 48),
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        var addBtn = SideBtn("+ 추가", AccentCl, 8);
        var delBtn = SideBtn("삭제",   DeleteCl, 100);
        addBtn.Click += OnAddClick;
        delBtn.Click += OnDelClick;
        btnBar.Controls.AddRange([addBtn, delBtn]);

        side.Controls.AddRange([border, _selDateLbl, divLine, _eventList, btnBar]);

        // 사이드 패널 내부 리사이즈 처리
        side.Resize += (_, _) =>
        {
            int sw = side.ClientSize.Width;
            int sh = side.ClientSize.Height;
            border     .SetBounds(0,  0,  1,    sh);
            _selDateLbl.SetBounds(1,  0,  sw-1, 44);
            divLine    .SetBounds(1,  44, sw-1, 1);
            _eventList .SetBounds(1,  45, sw-1, sh - 45 - 48);
            btnBar     .SetBounds(1,  sh - 48, sw-1, 48);
        };

        return side;
    }

    private static Button SideBtn(string t, Color bg, int x) => new()
    {
        Text      = t,
        Bounds    = new Rectangle(x, 8, 84, 32),
        FlatStyle = FlatStyle.Flat,
        Font      = new Font("Segoe UI", 9f),
        ForeColor = Color.White,
        BackColor = bg,
        Cursor    = Cursors.Hand,
        FlatAppearance = { BorderSize = 0 }
    };

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────
    private void ShiftMonth(int delta)
    {
        var m = _grid.CurrentMonth.AddMonths(delta);
        _grid.SetMonth(m.Year, m.Month);
        _grid.SetEvents(_ctrl.GetByMonth(m.Year, m.Month));
        UpdateMonthLabel();
    }

    private void OnDateSelected(object? sender, DateTime date)
    {
        _selDate = date;
        _selDateLbl.Text = date.ToString("yyyy년 M월 d일 (ddd)");
        RefreshEventList();
    }

    private void OnAddClick(object? sender, EventArgs e)
    {
        using var dlg = new EventEditDialog(_selDate);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _ctrl.Add(_selDate, dlg.EventTitle, dlg.EventDesc, dlg.EventStart, dlg.EventEnd);
            RefreshGrid();
            RefreshEventList();
        }
    }

    private void OnDelClick(object? sender, EventArgs e)
    {
        if (_eventList.SelectedItem is not CalendarEvent ev) return;
        if (MessageBox.Show($"'{ev.Title}' 을(를) 삭제할까요?", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _ctrl.Delete(ev.Id);
            RefreshGrid();
            RefreshEventList();
        }
    }

    // ── 갱신 ─────────────────────────────────────────────────────────────
    private void RefreshGrid()
    {
        var m = _grid.CurrentMonth;
        _grid.SetEvents(_ctrl.GetByMonth(m.Year, m.Month));
    }

    private void RefreshEventList()
    {
        _eventList.Items.Clear();
        foreach (var ev in _ctrl.GetByDate(_selDate))
            _eventList.Items.Add(ev);
    }

    private void UpdateMonthLabel()
    {
        if (_monthLbl != null)
            _monthLbl.Text = _grid.CurrentMonth.ToString("yyyy년  M월");
    }

    // ── 이벤트 항목 커스텀 드로우 ────────────────────────────────────────
    private static void DrawEventItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ListBox lb) return;
        var ev  = (CalendarEvent)lb.Items[e.Index];
        bool sel = (e.State & DrawItemState.Selected) != 0;

        using var bgBr = new SolidBrush(sel ? Color.FromArgb(230, 248, 244) : Color.White);
        e.Graphics.FillRectangle(bgBr, e.Bounds);

        using var barBr = new SolidBrush(AccentCl);
        e.Graphics.FillRectangle(barBr, e.Bounds.X + 1, e.Bounds.Y + 6, 4, e.Bounds.Height - 12);

        using var fgBr  = new SolidBrush(Color.FromArgb(44, 62, 80));
        using var timBr = new SolidBrush(Color.FromArgb(120, 140, 160));
        using var f     = new Font("Segoe UI", 9.5f);
        using var ft    = new Font("Segoe UI", 8.5f);

        float ty = e.Bounds.Y + (e.Bounds.Height - f.GetHeight()) / 2f;
        e.Graphics.DrawString(ev.Title, f, fgBr, e.Bounds.X + 12, ty);
        if (!string.IsNullOrEmpty(ev.StartTime))
            e.Graphics.DrawString(ev.StartTime, ft, timBr, e.Bounds.Right - 52, ty + 1);

        using var pen = new Pen(Color.FromArgb(240, 242, 245), 1);
        e.Graphics.DrawLine(pen, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }
}

// ── 일정 추가 다이얼로그 ──────────────────────────────────────────────────
internal class EventEditDialog : Form
{
    public string EventTitle { get; private set; } = "";
    public string EventDesc  { get; private set; } = "";
    public string EventStart { get; private set; } = "";
    public string EventEnd   { get; private set; } = "";

    private readonly TextBox _titleBox, _descBox, _startBox, _endBox;

    public EventEditDialog(DateTime date)
    {
        Text            = $"일정 추가  —  {date:yyyy년 M월 d일}";
        Size            = new Size(380, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false; MinimizeBox = false;
        BackColor       = Color.FromArgb(245, 246, 250);

        int y = 20;
        _titleBox = AddField("제목 *",             ref y);
        _startBox = AddField("시작 시간 (예: 09:00)", ref y);
        _endBox   = AddField("종료 시간 (예: 10:00)", ref y);
        _descBox  = AddField("메모",               ref y);

        var ok = new Button
        {
            Text = "저장", DialogResult = DialogResult.OK,
            Bounds = new Rectangle(190, y + 8, 80, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(26, 188, 156), ForeColor = Color.White,
            FlatAppearance = { BorderSize = 0 }
        };
        var cancel = new Button
        {
            Text = "취소", DialogResult = DialogResult.Cancel,
            Bounds = new Rectangle(278, y + 8, 78, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White,
            FlatAppearance = { BorderSize = 0 }
        };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_titleBox.Text))
            { MessageBox.Show("제목을 입력해주세요."); DialogResult = DialogResult.None; return; }
            EventTitle = _titleBox.Text.Trim();
            EventDesc  = _descBox.Text.Trim();
            EventStart = _startBox.Text.Trim();
            EventEnd   = _endBox.Text.Trim();
        };
        Controls.AddRange([ok, cancel]);
        AcceptButton = ok; CancelButton = cancel;
    }

    private TextBox AddField(string label, ref int y)
    {
        Controls.Add(new Label
        {
            Text = label, Bounds = new Rectangle(16, y, 340, 18),
            Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(80, 100, 120)
        });
        y += 20;
        var tb = new TextBox
        {
            Bounds = new Rectangle(16, y, 340, 26),
            Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(tb);
        y += 38;
        return tb;
    }
}
