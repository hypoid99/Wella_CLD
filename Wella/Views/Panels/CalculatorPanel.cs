namespace Wella.Views.Panels;

// Phase 5에서 구현 예정
public class CalculatorPanel : UserControl
{
    public CalculatorPanel()
    {
        BackColor = Color.FromArgb(255, 235, 220);   // 구분용 연주황
        Dock      = DockStyle.Fill;

        var lbl = new Label
        {
            Text      = "[ 계산기 ]  Phase 5 구현 예정",
            Font      = new Font("Segoe UI", 18f, FontStyle.Regular),
            ForeColor = Color.FromArgb(52, 73, 94),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        Controls.Add(lbl);
    }
}
