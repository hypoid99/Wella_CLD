using Wella.Controllers;
using Wella.Views.Controls;

namespace Wella.Views;

public partial class MainForm : Form
{
    private TopMenuBar    _topMenu       = null!;
    private Panel         _contentPanel  = null!;
    private AppController _appController = null!;

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

        _contentPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 246, 250)
        };

        Controls.Add(_contentPanel);
        Controls.Add(_topMenu);

        ResumeLayout(false);
    }

    public void UpdateActiveMenu(ToolType tool)
    {
        _topMenu.SetActiveTool(tool);
    }
}
