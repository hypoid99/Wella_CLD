using Wella.Views;
using Wella.Views.Panels;

namespace Wella.Controllers;

public enum ToolType
{
    Calendar,
    Todo,
    Memo,
    Calculator
}

public class AppController
{
    private readonly MainForm _mainForm;
    private readonly Dictionary<ToolType, UserControl> _panels;
    private UserControl? _currentPanel;

    public AppController(MainForm mainForm)
    {
        _mainForm = mainForm;
        _panels = new Dictionary<ToolType, UserControl>
        {
            { ToolType.Calendar,   new CalendarPanel()   },
            { ToolType.Todo,       new TodoPanel()       },
            { ToolType.Memo,       new MemoPanel()       },
            { ToolType.Calculator, new CalculatorPanel() }
        };

        foreach (var panel in _panels.Values)
        {
            panel.Dock = DockStyle.Fill;
            panel.Visible = false;
            _mainForm.ContentPanel.Controls.Add(panel);
        }
    }

    public void Initialize() => SwitchTool(ToolType.Calendar);

    public void SwitchTool(ToolType tool)
    {
        _currentPanel?.Hide();
        _currentPanel = _panels[tool];
        _currentPanel.Dock = DockStyle.Fill;
        _currentPanel.BringToFront();   // z-order 최상위로 올려야 Fill이 정상 동작
        _currentPanel.Show();
        _mainForm.ContentPanel.PerformLayout();
        _mainForm.UpdateActiveMenu(tool);
    }
}
