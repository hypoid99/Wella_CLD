using Wella.Controllers;
using Wella.Models;
using Wella.Views.Controls;

namespace Wella.Views.Panels;

public class CalendarPanel : UserControl
{
    private readonly CalendarController _ctrl;
    private CalendarGrid _grid   = null!;
    private Panel        _navBar = null!;
    private DateTime     _selDate = DateTime.Today;

    private Label _monthLbl = null!;

    private const int NavH = 48;

    private static readonly Color NavBg = Color.FromArgb(44, 62, 80);

    public CalendarPanel()
    {
        _ctrl = new CalendarController();
        BackColor = Color.FromArgb(245, 246, 250);
        BuildControls();
        RefreshGrid();
    }

    private void BuildControls()
    {
        _grid = new CalendarGrid();
        _grid.DateSelected += OnDateSelected;

        _navBar = BuildNavBar();

        Controls.Add(_grid);
        Controls.Add(_navBar);
    }

    private void ManualLayout()
    {
        if (_grid == null) return;
        int w = ClientSize.Width;
        int h = ClientSize.Height;

        _navBar.SetBounds(0, 0, w, NavH);
        _grid  .SetBounds(0, NavH, w, h - NavH);
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); ManualLayout(); }
    protected override void OnLoad(EventArgs e)   { base.OnLoad(e);   ManualLayout(); UpdateMonthLabel(); }

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

        using var dlg = new CalendarDayDialog(date, _ctrl);
        dlg.ShowDialog(this);

        RefreshGrid();
    }

    private void RefreshGrid()
    {
        var m = _grid.CurrentMonth;
        _grid.SetEvents(_ctrl.GetByMonth(m.Year, m.Month));
    }

    private void UpdateMonthLabel()
    {
        if (_monthLbl != null)
            _monthLbl.Text = _grid.CurrentMonth.ToString("yyyy년  M월");
    }
}

// ── 날짜별 일정 다이얼로그 ────────────────────────────────────────────────
internal class CalendarDayDialog : Form
{
    private readonly CalendarController _ctrl;
    private readonly DateTime           _date;
    private ListBox _list     = null!;
    private TextBox _titleBox = null!;
    private TextBox _descBox  = null!;

    private static readonly Color AccentCl = Color.FromArgb(26,  188, 156);
    private static readonly Color DeleteCl = Color.FromArgb(192,  57,  43);
    private static readonly Color DivColor = Color.FromArgb(218, 224, 232);
    private static readonly Color BgColor  = Color.FromArgb(245, 246, 250);

    public CalendarDayDialog(DateTime date, CalendarController ctrl)
    {
        _date = date;
        _ctrl = ctrl;

        Text            = $"{date:yyyy년 M월 d일 (ddd)}  일정";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;
        BackColor       = BgColor;

        BuildLayout();
        LoadEvents();
    }

    private void BuildLayout()
    {
        int y = 12;

        // ── 기존 일정 목록 ──
        AddSectionLabel("이 날의 일정", ref y);

        _list = new ListBox
        {
            Bounds         = new Rectangle(14, y, 348, 130),
            Font           = new Font("Segoe UI", 9.5f),
            BorderStyle    = BorderStyle.FixedSingle,
            BackColor      = Color.White,
            ItemHeight     = 28,
            IntegralHeight = false,
            DrawMode       = DrawMode.OwnerDrawFixed
        };
        _list.DrawItem += DrawListItem;
        Controls.Add(_list);
        y += 138;

        var delBtn = new Button
        {
            Text      = "선택 항목 삭제",
            Bounds    = new Rectangle(14, y, 120, 28),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.White,
            BackColor = DeleteCl,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        delBtn.Click += OnDeleteClick;
        Controls.Add(delBtn);
        y += 40;

        // ── 구분선 ──
        Controls.Add(new Panel { Bounds = new Rectangle(14, y, 348, 1), BackColor = DivColor });
        y += 12;

        // ── 새 일정 추가 ──
        AddSectionLabel("새 일정 추가", ref y);

        _titleBox = AddField("제목 *", ref y);
        _descBox  = AddField("메모",   ref y);

        // ── 하단 버튼 ──
        var saveBtn = new Button
        {
            Text      = "저장",
            Bounds    = new Rectangle(14, y + 4, 90, 34),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 10f),
            ForeColor = Color.White,
            BackColor = AccentCl,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        var closeBtn = new Button
        {
            Text         = "닫기",
            DialogResult = DialogResult.Cancel,
            Bounds       = new Rectangle(272, y + 4, 90, 34),
            FlatStyle    = FlatStyle.Flat,
            Font         = new Font("Segoe UI", 10f),
            ForeColor    = Color.FromArgb(44, 62, 80),
            BackColor    = Color.FromArgb(220, 223, 228),
            Cursor       = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        saveBtn.Click += OnSaveClick;
        Controls.Add(saveBtn);
        Controls.Add(closeBtn);

        CancelButton = closeBtn;
        ClientSize   = new Size(376, y + 50);
    }

    private void AddSectionLabel(string text, ref int y)
    {
        Controls.Add(new Label
        {
            Text      = text,
            Bounds    = new Rectangle(14, y, 348, 20),
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80)
        });
        y += 24;
    }

    private TextBox AddField(string label, ref int y)
    {
        Controls.Add(new Label
        {
            Text      = label,
            Bounds    = new Rectangle(14, y, 348, 18),
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(80, 100, 120)
        });
        y += 20;
        var tb = new TextBox
        {
            Bounds      = new Rectangle(14, y, 348, 26),
            Font        = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(tb);
        y += 38;
        return tb;
    }

    private void LoadEvents()
    {
        _list.Items.Clear();
        foreach (var ev in _ctrl.GetByDate(_date))
            _list.Items.Add(ev);
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_titleBox.Text))
        {
            MessageBox.Show("제목을 입력해주세요.", "알림",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _titleBox.Focus();
            return;
        }

        _ctrl.Add(_date,
            _titleBox.Text.Trim(),
            _descBox.Text.Trim(),
            "", "");

        _titleBox.Clear();
        _descBox.Clear();
        _titleBox.Focus();

        LoadEvents();
    }

    private void OnDeleteClick(object? sender, EventArgs e)
    {
        if (_list.SelectedItem is not CalendarEvent ev) return;

        if (MessageBox.Show($"'{ev.Title}' 을(를) 삭제할까요?", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _ctrl.Delete(ev.Id);
            LoadEvents();
        }
    }

    private static void DrawListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ListBox lb) return;
        var ev  = (CalendarEvent)lb.Items[e.Index];
        bool sel = (e.State & DrawItemState.Selected) != 0;

        using var bgBr = new SolidBrush(sel ? Color.FromArgb(230, 248, 244) : Color.White);
        e.Graphics.FillRectangle(bgBr, e.Bounds);

        using var barBr = new SolidBrush(AccentCl);
        e.Graphics.FillRectangle(barBr, e.Bounds.X, e.Bounds.Y + 4, 4, e.Bounds.Height - 8);

        using var fgBr = new SolidBrush(Color.FromArgb(44, 62, 80));
        using var f    = new Font("Segoe UI", 9.5f);

        float ty = e.Bounds.Y + (e.Bounds.Height - f.GetHeight()) / 2f;
        e.Graphics.DrawString(ev.Title, f, fgBr, e.Bounds.X + 10, ty);

        using var pen = new Pen(Color.FromArgb(240, 242, 245), 1);
        e.Graphics.DrawLine(pen, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }
}
