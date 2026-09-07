using System.Configuration;
using System.Data;
using System.Windows;

using System.Threading.Tasks;
using System.Windows.Threading;
using DevGitAtom.Engine.Logging;

namespace DevGitAtom.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length > 0 && e.Args[0] == "--askpass")
        {
            var prompt = e.Args.Length > 1 ? e.Args[1] : "Git đang yêu cầu xác thực:";
            var pass = ShowAskPassDialog(prompt);
            using (var sw = new System.IO.StreamWriter(Console.OpenStandardOutput()))
            {
                sw.Write(pass + "\n");
            }
            Environment.Exit(0);
        }

        base.OnStartup(e);

        JsonLogger.LogInfo("App_Startup", "Application started.");

        // Bắt lỗi trên luồng UI chính
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;

        // Bắt lỗi từ các Task ngầm
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        // Bắt lỗi toàn cục của AppDomain
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        JsonLogger.LogError("App_DispatcherError", e.Exception.Message, e.Exception);
        e.Handled = true; // Cố gắng không crash app
        
        // Cố gắng hiển thị cảnh báo UI nếu có thể
        if (MainWindow is UI.Windows.MainWindow mw)
        {
            mw.ShowNotification("Lỗi hệ thống! Vui lòng kiểm tra file log.", isError: true);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        JsonLogger.LogError("App_UnobservedTaskError", e.Exception.Message, e.Exception);
        e.SetObserved();
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        JsonLogger.LogError("App_FatalCrash", ex?.Message ?? "Lỗi không xác định", ex);
    }

    private string ShowAskPassDialog(string promptMessage)
    {
        string result = "";
        var window = new Window
        {
            Title = "Yêu cầu xác thực Git (AskPass)",
            Width = 400,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Topmost = true,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(13, 17, 23))
        };
        
        var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(15) };
        var text = new System.Windows.Controls.TextBlock 
        { 
            Text = promptMessage, 
            Foreground = System.Windows.Media.Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10) 
        };
        var passBox = new System.Windows.Controls.PasswordBox { Margin = new Thickness(0, 0, 0, 15) };
        var btn = new System.Windows.Controls.Button { Content = "OK", Width = 80, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, IsDefault = true };
        
        btn.Click += (s, ev) => { result = passBox.Password; window.DialogResult = true; };
        
        stack.Children.Add(text);
        stack.Children.Add(passBox);
        stack.Children.Add(btn);
        window.Content = stack;
        
        window.ShowDialog();
        return result;
    }
}






