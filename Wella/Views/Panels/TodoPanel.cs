namespace Wella.Views.Panels;

// Phase 3에서 구현 예정
public class TodoPanel : UserControl
{
    public TodoPanel()
    {
        BackColor = Color.FromArgb(230, 255, 240);   // 구분용 연초록
        Dock      = DockStyle.Fill;

        var lbl = new Label
        {
            Text      = "[ 할일 ]  Phase 3 구현 예정",
            Font      = new Font("Segoe UI", 18f, FontStyle.Regular),
            ForeColor = Color.FromArgb(52, 73, 94),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        Controls.Add(lbl);
    }
}
