using Wella.Controllers;
using Wella.Views.Controls;

namespace Wella.Views;

public partial class MainForm : Form
{
    private TopMenuBar     _topMenu      = null!;
    private SidebarControl _sidebar      = null!;
    private Panel          _contentPanel = null!;
    private AppController  _appController = null!;

    public Panel ContentPanel => _contentPanel;

    public MainForm()
    {
        InitializeComponent();
        BuildLayout();

        // AppController 생성 (패널을 ContentPanel에 등록)
        _appController = new AppController(this);

        // Form이 완전히 배치된 후 초기 화면 표시
        Load += (_, _) => _appController.Initialize();
    }

    private void BuildLayout()
    {
        Text          = "Wella";
        Size          = new Size(1100, 720);
        MinimumSize   = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor     = Color.FromArgb(245, 246, 250);

        SuspendLayout();

        // ── 상단 메뉴바 (Top) ────────────────────────────
        _topMenu = new TopMenuBar { Dock = DockStyle.Top, Height = 56 };
        _topMenu.ToolClicked += (_, tool) => _appController?.SwitchTool(tool);

        // ── body 컨테이너 (Fill) ─────────────────────────
        var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 246, 250) };

        _sidebar = new SidebarControl { Dock = DockStyle.Left, Width = 180 };
        _sidebar.ToolClicked += (_, tool) => _appController?.SwitchTool(tool);

        _contentPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 246, 250)
        };

        // 순서 중요: Fill 먼저 추가 → Left가 공간을 먼저 차지한 뒤 Fill이 나머지를 채움
        body.Controls.Add(_contentPanel);   // Fill
        body.Controls.Add(_sidebar);         // Left (Fill보다 나중 추가 = 레이아웃 우선 처리)

        // Form 컨트롤 추가 순서: body(Fill) → topMenu(Top)
        Controls.Add(body);
        Controls.Add(_topMenu);

        ResumeLayout(false);
    }

    public void UpdateActiveMenu(ToolType tool)
    {
        _topMenu.SetActiveTool(tool);
        _sidebar.SetActiveTool(tool);
    }
}
