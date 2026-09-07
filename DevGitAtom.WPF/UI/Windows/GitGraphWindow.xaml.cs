using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace DevGitAtom.WPF.UI.Windows;

public partial class GitGraphWindow : Window
{
    private readonly string _workingDir;

    public GitGraphWindow(string workingDir)
    {
        InitializeComponent();
        _workingDir = workingDir;
        Loaded += GitGraphWindow_Loaded;
    }

    private int _skip = 0;
    private const int _limit = 40;
    private bool _isLoading = false;

    private async void GitGraphWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await GraphWebView.EnsureCoreWebView2Async(null);

        // Bind WebMessage to C#
        GraphWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <script src='https://cdn.jsdelivr.net/npm/xterm@5.3.0/lib/xterm.min.js'></script>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/xterm@5.3.0/css/xterm.css'/>
    <style>
        body { margin: 0; background: #0d1117; padding: 10px; overflow: hidden; }
        #terminal { width: 100%; height: 100vh; }
        .xterm-viewport { background-color: #0d1117 !important; }
    </style>
</head>
<body>
    <div id='terminal'></div>
    <script>
        var term = new Terminal({
            theme: { background: '#0d1117', foreground: '#e6edf3' },
            fontFamily: 'Consolas, Courier New, monospace',
            fontSize: 14,
            convertEol: true,
            scrollback: 99999
        });
        term.open(document.getElementById('terminal'));

        window.writeGraph = (text) => {
            let polished = text.replace(/\*/g, '\x1b[38;5;214m●\x1b[0m');
            term.write(polished);
        };

        window.scrollToTop = () => { term.scrollToTop(); };

        // Listen for keypress to load more (Enter or DownArrow)
        term.onKey(e => {
            if (e.key === '\r' || e.key === '\x1b[B' || e.domEvent.keyCode === 34) { // Enter, Down, PageDown
                window.chrome.webview.postMessage('load_more');
            }
        });

        term.write('\x1b[36mNhấn Enter hoặc Mũi tên xuống để tải thêm commit...\x1b[0m\r\n\r\n');
    </script>
</body>
</html>";

        GraphWebView.NavigateToString(html);

        // Wait for navigation to complete before injecting data
        var tcs = new TaskCompletionSource<bool>();
        void OnNavigated(object? s, CoreWebView2NavigationCompletedEventArgs args)
        {
            GraphWebView.CoreWebView2.NavigationCompleted -= OnNavigated;
            tcs.SetResult(true);
        }
        GraphWebView.CoreWebView2.NavigationCompleted += OnNavigated;
        await tcs.Task;

        await LoadGitGraphAsync(isFirstLoad: true);
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.TryGetWebMessageAsString() == "load_more")
        {
            if (!_isLoading)
            {
                _skip += _limit;
                await LoadGitGraphAsync(isFirstLoad: false);
            }
        }
    }

    private async Task LoadGitGraphAsync(bool isFirstLoad)
    {
        _isLoading = true;
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"--no-pager log --graph --all --oneline --decorate --color=always --skip={_skip} -n {_limit}",
            WorkingDirectory = _workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null) return;

            string output = await proc.StandardOutput.ReadToEndAsync();
            string error = await proc.StandardError.ReadToEndAsync();

            await proc.WaitForExitAsync();

            if (!string.IsNullOrEmpty(error))
            {
                var errJson = System.Text.Json.JsonSerializer.Serialize($"\x1b[31m{error}\x1b[0m\r\n");
                await GraphWebView.CoreWebView2.ExecuteScriptAsync($"window.writeGraph({errJson});");
            }

            if (!string.IsNullOrEmpty(output))
            {
                // Ensure properly formatted newlines for xterm
                output = output.Replace("\r\n", "\n").Replace("\n", "\r\n");
                var outJson = System.Text.Json.JsonSerializer.Serialize(output);
                await GraphWebView.CoreWebView2.ExecuteScriptAsync($"window.writeGraph({outJson});");
            }

            if (isFirstLoad)
            {
                // Force scroll to top on first load so user sees latest commit
                await GraphWebView.CoreWebView2.ExecuteScriptAsync("window.scrollToTop();");
            }
        }
        catch (Exception ex)
        {
            var exJson = System.Text.Json.JsonSerializer.Serialize($"\x1b[31m[Lỗi hệ thống: {ex.Message}]\x1b[0m\r\n");
            await GraphWebView.CoreWebView2.ExecuteScriptAsync($"window.writeGraph({exJson});");
        }
        finally
        {
            _isLoading = false;
        }
    }
}
