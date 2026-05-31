using Wella.Views;

namespace Wella;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // 처리되지 않은 예외를 메시지박스로 표시
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.ToString(), "오류 발생", MessageBoxButtons.OK, MessageBoxIcon.Error);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            MessageBox.Show(e.ExceptionObject.ToString(), "치명적 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
