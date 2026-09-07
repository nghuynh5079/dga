using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using DevGitAtom.Contracts;
using DevGitAtom.Engine;
using DevGitAtom.GitAtoms;
using DevGitAtom.GitAtoms.Atoms;
using DevGitAtom.Engine.Logging;

using Microsoft.Web.WebView2.Core;


using DevGitAtom.WPF.UI.Dialogs;

namespace DevGitAtom.WPF.UI.Windows;

public partial class MainWindow : Window
{
    private readonly AtomRegistry _registry;
    private readonly PresetManager _presetManager;
    private readonly WorkspaceManager _workspaceManager;
    private readonly HistoryManager _historyManager;
    private readonly List<ChainStep> _chain = [];
    private string _workingDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private CancellationTokenSource? _cts;
    private bool _terminalReady = false;
    private int _dragSourceIndex = -1;

    public MainWindow()
    {
        InitializeComponent();
        InitializeAsync();
        _registry = new AtomRegistry();
        _presetManager = new PresetManager();
        _workspaceManager = new WorkspaceManager();
        _historyManager = new HistoryManager();
        RegisterAtoms();
        BuildAtomListUI();
        RefreshRecentWorkspaces();
        TxtWorkingDir.Text = $"📁 {_workingDir}";
        RefreshBranchInfo();
    }

    private PseudoConsole? _pty;

    private async void InitializeAsync()
    {
        await TerminalWebView.EnsureCoreWebView2Async(null);
        TerminalWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/xterm@5.3.0/css/xterm.css"" />
    <script src=""https://cdn.jsdelivr.net/npm/xterm@5.3.0/lib/xterm.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/xterm-addon-fit@0.8.0/lib/xterm-addon-fit.js""></script>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        html, body { width: 100%; height: 100%; background: #0d1117; overflow: hidden; }
        #terminal { width: 100%; height: 100%; padding: 6px; }
        .xterm { height: 100%; }
    </style>
</head>
<body>
    <div id=""terminal""></div>
    <script>
        const term = new Terminal({
            theme: { background: '#0d1117', foreground: '#e6edf3', cursor: '#58a6ff' },
            fontFamily: 'Consolas, Courier New, monospace',
            fontSize: 13,
            cursorBlink: true,
            scrollback: 5000
        });

        const fitAddon = new FitAddon.FitAddon();
        term.loadAddon(fitAddon);
        term.open(document.getElementById('terminal'));

        function doFit() { try { fitAddon.fit(); } catch(e) {} }
        setTimeout(doFit, 50);
        window.addEventListener('resize', doFit);

        document.addEventListener('mousedown',   () => term.focus());
        document.addEventListener('pointerdown', () => term.focus());

        window.writeToTerminal = function(text) { term.write(text); };
        window.clearTerminal   = function()     { term.clear(); term.focus(); };
        window.fitTerminal     = function()     { doFit(); term.focus(); };

        term.onData(e => { window.chrome.webview.postMessage(e); });
        term.focus();
    </script>
</body>
</html>";
        TerminalWebView.NavigateToString(html);
        _terminalReady = true;
        
        await Task.Delay(500);
        StartPty();
        JsonLogger.LogInfo("UI", "MainWindow Webview and Pty initialized.");
    }

    private void StartPty()
    {
        try
        {
            _pty?.Dispose();
            _pty = new PseudoConsole();
            _pty.OutputReceived += (s, text) => 
            {
                _ = Dispatcher.InvokeAsync(() => WriteToTerminalAsync(text));
            };
            _pty.Start("cmd.exe", _workingDir);
        }
        catch (Exception ex)
        {
            _ = WriteToTerminalAsync($"\x1b[31m[PTY Error: {ex.Message}]\x1b[0m\r\n");
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var input = e.TryGetWebMessageAsString();
        if (!string.IsNullOrEmpty(input))
        {
            _pty?.Write(input);
        }
    }

    private async Task WriteToTerminalAsync(string text)
    {
        if (!_terminalReady) return;
        var json = System.Text.Json.JsonSerializer.Serialize(text);
        await TerminalWebView.CoreWebView2.ExecuteScriptAsync($"window.writeToTerminal({json})");
    }

    private async void BtnClearTerminal_Click(object sender, RoutedEventArgs e)
    {
        if (_terminalReady)
            await TerminalWebView.CoreWebView2.ExecuteScriptAsync("window.clearTerminal()");
    }

    // ═══════════════════════════════════════════════
    // ATOM REGISTRY SETUP
    // ═══════════════════════════════════════════════

    private void RegisterAtoms()
    {
        _registry.Register(new PreconditionAtom());
        _registry.Register(new CheckoutAtom());
        _registry.Register(new BranchAtom());
        _registry.Register(new AddAtom());
        _registry.Register(new FetchAtom());
        _registry.Register(new PullAtom());
        _registry.Register(new PushAtom());
        _registry.Register(new CommitAtom());
        _registry.Register(new MergeAtom());
        _registry.Register(new RebaseAtom());
        _registry.Register(new ResetAtom());
        _registry.Register(new RevertAtom());
        _registry.Register(new CherryPickAtom());
        _registry.Register(new TagAtom());
        _registry.Register(new CleanAtom());
        _registry.Register(new LogAtom());
        _registry.Register(new StashAtom());
        _registry.Register(new StatusAtom());
        
        _registry.Register(new BranchProtectionAtom());
        _registry.Register(new DryRunAtom());
        _registry.Register(new SubmoduleUpdateAtom());
        _registry.Register(new WorktreeAtom());
    }

    private void BuildAtomListUI()
    {
        AtomListPanel.Children.Clear();
        foreach (var category in Enum.GetValues<AtomCategory>())
        {
            var atomsInCategory = _registry.GetAll().Where(a => a.Category == category).ToList();
            if (atomsInCategory.Count == 0) continue;

            var header = new TextBlock
            {
                Text = category.ToString().ToUpper(),
                Foreground = (System.Windows.Media.Brush)FindResource("TextMuted"),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Margin = new Thickness(8, 16, 0, 12)
            };
            AtomListPanel.Children.Add(header);

            var wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal
            };

            foreach (var atom in atomsInCategory)
            {
                var btn = new Button
                {
                    Content = $"+ {atom.DisplayName}",
                    Tag = atom.Id,
                    Style = FindResource("AtomButton") as Style,
                    Margin = new Thickness(8, 0, 8, 12),
                    Width = 160,
                    Height = 44,
                    FontSize = 13,
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center
                };
                btn.Click += AtomButton_Click;
                wrapPanel.Children.Add(btn);
            }
            
            AtomListPanel.Children.Add(wrapPanel);
        }
    }

    // ═══════════════════════════════════════════════
    // ATOM BUTTON — Add to Chain
    // ═══════════════════════════════════════════════

    private void AtomButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string atomId)
        {
            _chain.Add(new ChainStep { AtomId = atomId });
            RebuildChainUI();
        }
    }

    // ═══════════════════════════════════════════════
    // CHAIN PANEL UI + DRAG & DROP REORDER
    // ═══════════════════════════════════════════════

    private void RebuildChainUI()
    {
        ChainPanel.Children.Clear();
        TxtChainEmpty.Visibility = _chain.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        BtnRun.IsEnabled = _chain.Count > 0 && !string.IsNullOrEmpty(_workingDir);

        for (int i = 0; i < _chain.Count; i++)
        {
            var idx = i;
            var step = _chain[i];
            var atom = _registry.Get(step.AtomId);

            var itemBorder = new Border
            {
                Style = FindResource("ChainItem") as Style,
                Tag = idx,
                AllowDrop = true,
            };

            // Drag events for reorder
            itemBorder.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _dragSourceIndex = (int)((Border)s).Tag;
                    DragDrop.DoDragDrop((Border)s, step.AtomId, DragDropEffects.Move);
                }
            };
            itemBorder.DragOver += ChainItem_DragOver;
            itemBorder.Drop += ChainItem_Drop;

            // Content
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });

            // Step number
            var stepNum = new TextBlock
            {
                Text = $"{idx + 1}",
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBlue"),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            };
            Grid.SetColumn(stepNum, 0);

            // Atom name
            var nameBlock = new TextBlock
            {
                Text = atom.DisplayName,
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimary"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(nameBlock, 1);

            if (atom.Parameters.Any())
            {
                var configBtn = new Button
                {
                    Content = "⚙️",
                    Tag = idx,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontSize = 12,
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    ToolTip = "Cấu hình tham số"
                };
                configBtn.Click += ConfigBtn_Click;
                Grid.SetColumn(configBtn, 2);
                row.Children.Add(configBtn);
            }

            // Remove button
            var removeBtn = new Button
            {
                Content = "✕",
                Tag = idx,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = (System.Windows.Media.Brush)FindResource("TextMuted"),
                BorderThickness = new Thickness(0),
                FontSize = 12,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            };
            removeBtn.Click += (s, _) => { _chain.RemoveAt((int)((Button)s).Tag); RebuildChainUI(); };
            Grid.SetColumn(removeBtn, 3);

            row.Children.Add(stepNum);
            row.Children.Add(nameBlock);
            row.Children.Add(removeBtn);
            itemBorder.Child = row;
            ChainPanel.Children.Add(itemBorder);
        }
    }

    private void ConfigBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int idx)
        {
            var step = _chain[idx];
            var atom = _registry.Get(step.AtomId);
            
            var dialog = new ConfigDialog(atom, step) { Owner = this };
            dialog.ShowDialog();
        }
    }

    private void ChainPanel_DragOver(object sender, DragEventArgs e) => e.Effects = DragDropEffects.Move;
    private void ChainItem_DragOver(object sender, DragEventArgs e) => e.Effects = DragDropEffects.Move;

    private void ChainItem_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border border && _dragSourceIndex >= 0)
        {
            var targetIdx = (int)border.Tag;
            if (targetIdx == _dragSourceIndex) return;

            var item = _chain[_dragSourceIndex];
            _chain.RemoveAt(_dragSourceIndex);
            _chain.Insert(targetIdx, item);
            _dragSourceIndex = -1;
            RebuildChainUI();
        }
    }

    private void ChainPanel_Drop(object sender, DragEventArgs e) { }

    // ═══════════════════════════════════════════════
    // RUN CHAIN
    // ═══════════════════════════════════════════════

    private bool _isRunning = false;

    private async void BtnRun_Click(object sender, RoutedEventArgs e)
    {
        JsonLogger.LogInfo("Action", "User clicked Run Chain");
        if (_isRunning)
        {
            _cts?.Cancel();
            BtnRun.Content = "⏳ Đang hủy...";
            BtnRun.IsEnabled = false;
            return;
        }

        if (_chain.Count == 0) return;

        // XÁC THỰC DEPENDENCY GRAPH (Requires/Provides)
        var validator = new ChainValidator(_registry);
        var validationResult = validator.Validate(_chain);

        if (!validationResult.IsValid)
        {
            var warningMsg = "Phát hiện một số cảnh báo trong chuỗi Atom của bạn:\n\n" + 
                             string.Join("\n", validationResult.Warnings) + 
                             "\n\nBạn có chắc chắn muốn chạy không? (Có thể dẫn đến lỗi Git)";
                             
            var result = MessageBox.Show(warningMsg, "Cảnh báo Dependency", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return; // User hủy
            }
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _isRunning = true;
        BtnRun.Content = "❌ CANCEL CHAIN";

        var config = new AppConfig { WorkingDir = _workingDir };
        var context = new WorkflowContext { Config = config };
        var invoker = new AtomInvoker(_registry);

        var progress = new Progress<string>(async line =>
        {
            await Dispatcher.InvokeAsync(() => WriteToTerminalAsync(line + "\r\n"));
        });

        await WriteToTerminalAsync("\x1b[2m─────────────────────────────\x1b[0m\r\n");
        var chainSteps = string.Join(" → ", _chain.Select(s => s.AtomId));
        await WriteToTerminalAsync($"\x1b[1;34m▶ Chain [{chainSteps}]\x1b[0m\r\n");

        try
        {
            var results = await invoker.RunChainAsync(_chain, context, progress, _cts.Token);

            await WriteToTerminalAsync("\r\n\x1b[2m─── Tóm tắt ──────────────────\x1b[0m\r\n");
            bool isSuccess = true;
            foreach (var r in results)
            {
                if (r.Outcome == AtomOutcome.Failed) isSuccess = false;
                var color = r.Outcome switch
                {
                    AtomOutcome.Completed => "\x1b[32m",
                    AtomOutcome.Skipped => "\x1b[33m",
                    _ => "\x1b[31m",
                };
                await WriteToTerminalAsync($"  {color}{r.AtomId}: {r.Outcome}\x1b[0m\r\n");
            }
            
            _historyManager.AddRecord(new RunRecord 
            {
                Timestamp = DateTime.Now,
                Workspace = _workingDir,
                ChainSummary = chainSteps,
                IsSuccess = isSuccess,
            });
        }
        catch (OperationCanceledException)
        {
            await WriteToTerminalAsync("\x1b[33m[CANCELLED]\x1b[0m\r\n");
        }
        finally
        {
            await RefreshBranchInfoAsync();
            _isRunning = false;
            BtnRun.IsEnabled = true;
            BtnRun.Content = "▶  RUN CHAIN";
        }
    }

    // ═══════════════════════════════════════════════
    // WINDOW CONTROLS
    // ═══════════════════════════════════════════════


    private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void MenuGitGraph_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_workingDir))
        {
            MessageBox.Show("Vui lòng chọn thư mục Git trước!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var graphWindow = new GitGraphWindow(_workingDir)
        {
            Owner = this
        };
        graphWindow.Show();
    }

    private void MenuHistory_Click(object sender, RoutedEventArgs e)
    {
        var historyWindow = new HistoryWindow()
        {
            Owner = this
        };
        historyWindow.Show();
    }

    private void MenuThemeDark_Click(object sender, RoutedEventArgs e)
    {
        MenuThemeDark.IsChecked = true;
        MenuThemeLight.IsChecked = false;
        ApplyTheme("DarkTheme");
    }

    private void MenuThemeLight_Click(object sender, RoutedEventArgs e)
    {
        MenuThemeDark.IsChecked = false;
        MenuThemeLight.IsChecked = true;
        ApplyTheme("LightTheme");
    }

    private static void ApplyTheme(string themeName)
    {
        var dict = new ResourceDictionary { Source = new Uri($"UI/Styles/Themes/{themeName}.xaml", UriKind.Relative) };
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(dict);
    }

    // ═══════════════════════════════════════════════
    // TOP BAR ACTIONS
    // ═══════════════════════════════════════════════

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Chọn thư mục Git repository",
            UseDescriptionForTitle = true,
            SelectedPath = _workingDir,
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            SetWorkingDir(dialog.SelectedPath);
        }
    }

    private void SetWorkingDir(string path)
    {
        JsonLogger.LogInfo("Workspace", $"Changed working directory to: {path}");
        _workingDir = path;
        TxtWorkingDir.Text = $"📁 {_workingDir}";
        _workspaceManager.AddWorkspace(_workingDir);
        RefreshRecentWorkspaces();
        RefreshBranchInfo();
        _ = WriteToTerminalAsync($"\x1b[2mThư mục: {_workingDir}\x1b[0m");
    }

    private void RefreshRecentWorkspaces()
    {
        MenuRecentWorkspaces.Items.Clear();
        var workspaces = _workspaceManager.GetRecentWorkspaces();
        
        if (workspaces.Count == 0)
        {
            MenuRecentWorkspaces.Items.Add(new System.Windows.Controls.MenuItem { Header = "(Trống)", IsEnabled = false });
            return;
        }

        foreach (var ws in workspaces)
        {
            var item = new System.Windows.Controls.MenuItem { Header = ws };
            item.Click += (s, e) => SetWorkingDir(ws);
            MenuRecentWorkspaces.Items.Add(item);
        }
    }

    private void RefreshBranchInfo() => _ = RefreshBranchInfoAsync();

    private async Task RefreshBranchInfoAsync()
    {
        try
        {
            var branch = await GitRunner.GetCurrentBranchAsync(_workingDir);
            var dirty = await GitRunner.HasUncommittedChangesAsync(_workingDir);
            Dispatcher.Invoke(() =>
            {
                TxtBranch.Text = $"⎇ {branch}";
                TxtDirtyIndicator.Text = dirty ? "●" : "";
            });
        }
        catch
        {
            Dispatcher.Invoke(() =>
            {
                TxtBranch.Text = "(không phải Git repo)";
                TxtDirtyIndicator.Text = "";
            });
        }
    }

    private void BtnClearChain_Click(object sender, RoutedEventArgs e)
    {
        _chain.Clear();
        RebuildChainUI();
    }

    private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
    {
        if (_chain.Count == 0)
        {
            ShowNotification("Chain đang trống, không có gì để lưu!", isError: true);
            return;
        }

        var dialog = new InputDialog("My Preset") { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            JsonLogger.LogInfo("Preset", $"User saved preset: {dialog.InputText}");
            _presetManager.SavePreset(dialog.InputText, _chain);
            ShowNotification($"Đã lưu preset: {dialog.InputText}");
        }
    }

    private void BtnLoadPreset_Click(object sender, RoutedEventArgs e)
    {
        var presets = _presetManager.GetAvailablePresets();
        PresetContextMenu.Items.Clear();

        if (presets.Count == 0)
        {
            PresetContextMenu.Items.Add(new System.Windows.Controls.MenuItem { Header = "(Chưa có preset nào)", IsEnabled = false });
        }
        else
        {
            foreach (var preset in presets)
            {
                var parentMenu = new System.Windows.Controls.MenuItem { Header = preset };
                
                var mnuLoad = new System.Windows.Controls.MenuItem { Header = "Load" };
                mnuLoad.Click += (s, args) => 
                {
                    var loadedChain = _presetManager.LoadPreset(preset);
                    _chain.Clear();
                    _chain.AddRange(loadedChain);
                    RebuildChainUI();
                };
                
                var mnuRename = new System.Windows.Controls.MenuItem { Header = "Rename..." };
                mnuRename.Click += (s, args) => 
                {
                    var dialog = new InputDialog(preset) { Owner = this };
                    if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
                    {
                        _presetManager.RenamePreset(preset, dialog.InputText);
                        ShowNotification($"Đã đổi tên thành: {dialog.InputText}");
                    }
                };
                
                var mnuDelete = new System.Windows.Controls.MenuItem { Header = "Delete", Foreground = (System.Windows.Media.Brush)FindResource("AccentRed") };
                mnuDelete.Click += (s, args) => 
                {
                    if (MessageBox.Show($"Bạn có chắc muốn xóa preset '{preset}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        _presetManager.DeletePreset(preset);
                        ShowNotification($"Đã xóa preset: {preset}");
                    }
                };
                
                parentMenu.Items.Add(mnuLoad);
                parentMenu.Items.Add(mnuRename);
                parentMenu.Items.Add(new Separator());
                parentMenu.Items.Add(mnuDelete);

                PresetContextMenu.Items.Add(parentMenu);
            }
        }
        
        PresetContextMenu.PlacementTarget = BtnLoadPreset;
        PresetContextMenu.IsOpen = true;
    }

    // ═══════════════════════════════════════════════
    // UI HELPERS
    // ═══════════════════════════════════════════════
    public async void ShowNotification(string message, bool isError = false)
    {
        NotificationMessage.Text = message;
        NotificationIcon.Text = isError ? "✕" : "✓";
        NotificationIcon.Foreground = isError 
            ? (SolidColorBrush)FindResource("AccentRed") 
            : (SolidColorBrush)FindResource("AccentGreen");
        
        NotificationToast.Visibility = Visibility.Visible;
        
        await Task.Delay(3000);
        NotificationToast.Visibility = Visibility.Collapsed;
    }
}


