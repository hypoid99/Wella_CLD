namespace Wella.Views.Panels;

// Phase 4에서 구현 예정
public class MemoPanel : UserControl
{
    public MemoPanel()
    {
        BackColor = Color.FromArgb(255, 255, 220);   // 구분용 연노랑
        Dock      = DockStyle.Fill;

        var lbl = new Label
        {
            Text      = "[ 메모 ]  Phase 4 구현 예정",
            Font      = new Font("Segoe UI", 18f, FontStyle.Regular),
            ForeColor = Color.FromArgb(52, 73, 94),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        Controls.Add(lbl);
    }
}
