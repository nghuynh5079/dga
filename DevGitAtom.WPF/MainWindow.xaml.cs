using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DevGitAtom.Core;
using DevGitAtom.Core.Atoms;
using Microsoft.Web.WebView2.Core;

namespace DevGitAtom.WPF;

public partial class MainWindow : Window
{
    private readonly AtomRegistry _registry = new();
    private readonly List<string> _chain = [];
    private string _workingDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private CancellationTokenSource? _cts;
    private bool _terminalReady = false;

    // For drag-reorder in chain
    private int _dragSourceIndex = -1;

    public MainWindow()
    {
        InitializeComponent();
        RegisterAtoms();
        BuildAtomListUI();
        _ = InitTerminalAsync();
        RefreshBranchInfo();
    }

    // ═══════════════════════════════════════════════
    // ATOM REGISTRY SETUP
    // ═══════════════════════════════════════════════

    private void RegisterAtoms()
    {
        _registry.Register(new PreconditionAtom());
        _registry.Register(new FetchAtom());
        _registry.Register(new PullAtom());
        _registry.Register(new StashAtom());
        _registry.Register(new StatusAtom());
        // More atoms can be added here
    }

    private void BuildAtomListUI()
    {
        AtomListPanel.Children.Clear();
        var categories = _registry.GetAll()
            .GroupBy(a => a.Category)
            .OrderBy(g => g.Key);

        foreach (var group in categories)
        {
            // Category header
            var header = new TextBlock
            {
                Text = group.Key.ToString().ToUpper(),
                Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0xa6, 0xff)),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(4, 10, 4, 4),
            };
            AtomListPanel.Children.Add(header);

            foreach (var atom in group)
            {
                var btn = new Button
                {
                    Tag = atom.Id,
                    Style = FindResource("AtomButton") as Style,
                };

                var mutatingBadge = atom.Mutating
                    ? new TextBlock { Text = " ●", Foreground = new SolidColorBrush(Color.FromRgb(0xd2, 0x99, 0x22)), FontSize = 10 }
                    : new TextBlock { Text = " ○", Foreground = new SolidColorBrush(Color.FromRgb(0x3f, 0xb9, 0x50)), FontSize = 10 };

                var content = new StackPanel { Orientation = Orientation.Horizontal };
                content.Children.Add(new TextBlock
                {
                    Text = atom.DisplayName,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xe6, 0xed, 0xf3)),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                });
                content.Children.Add(mutatingBadge);

                btn.Content = content;
                btn.Click += AtomButton_Click;
                btn.ToolTip = atom.Mutating ? "[MUTATING] Thay đổi Git state" : "[read-only] Chỉ đọc";
                AtomListPanel.Children.Add(btn);
            }
        }
    }

    // ═══════════════════════════════════════════════
    // TERMINAL (WebView2 + xterm.js)
    // ═══════════════════════════════════════════════

    private async Task InitTerminalAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevGitAtom", "WebView2");

        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await TerminalWebView.EnsureCoreWebView2Async(env);

        TerminalWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "terminal.html");
        TerminalWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
        TerminalWebView.CoreWebView2.NavigationCompleted += (_, _) => { _terminalReady = true; };
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Handle user input typed in terminal (for raw git commands)
        // Future: pipe to a real PTY/shell process
    }

    private async Task WriteToTerminalAsync(string text)
    {
        if (!_terminalReady) return;
        // Escape backslashes and single quotes for JS
        var escaped = text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r\n", "\\r\\n").Replace("\n", "\\r\\n");
        await TerminalWebView.CoreWebView2.ExecuteScriptAsync($"window.writeToTerminal('{escaped}\\r\\n')");
    }

    private async void BtnClearTerminal_Click(object sender, RoutedEventArgs e)
    {
        if (_terminalReady)
            await TerminalWebView.CoreWebView2.ExecuteScriptAsync("window.clearTerminal()");
    }

    // ═══════════════════════════════════════════════
    // ATOM BUTTON — Add to Chain
    // ═══════════════════════════════════════════════

    private void AtomButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string atomId)
        {
            _chain.Add(atomId);
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
            var atomId = _chain[i];
            var atom = _registry.Get(atomId);

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
                    DragDrop.DoDragDrop((Border)s, atomId, DragDropEffects.Move);
                }
            };
            itemBorder.DragOver += ChainItem_DragOver;
            itemBorder.Drop += ChainItem_Drop;

            // Content
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });

            // Step number
            var stepNum = new TextBlock
            {
                Text = $"{idx + 1}",
                Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0xa6, 0xff)),
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
                Foreground = new SolidColorBrush(Color.FromRgb(0xe6, 0xed, 0xf3)),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(nameBlock, 1);

            // Remove button
            var removeBtn = new Button
            {
                Content = "✕",
                Tag = idx,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8b, 0x94, 0x9e)),
                BorderThickness = new Thickness(0),
                FontSize = 12,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            };
            removeBtn.Click += (s, _) => { _chain.RemoveAt((int)((Button)s).Tag); RebuildChainUI(); };
            Grid.SetColumn(removeBtn, 2);

            row.Children.Add(stepNum);
            row.Children.Add(nameBlock);
            row.Children.Add(removeBtn);
            itemBorder.Child = row;
            ChainPanel.Children.Add(itemBorder);
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

    private async void BtnRun_Click(object sender, RoutedEventArgs e)
    {
        if (_chain.Count == 0) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        BtnRun.IsEnabled = false;
        BtnRun.Content = "⏳  Running...";

        var config = new AppConfig { WorkingDir = _workingDir };
        var context = new WorkflowContext { Config = config };
        var invoker = new AtomInvoker(_registry);

        var progress = new Progress<string>(async line =>
        {
            await Dispatcher.InvokeAsync(() => WriteToTerminalAsync(line));
        });

        await WriteToTerminalAsync("\x1b[2m─────────────────────────────\x1b[0m");
        await WriteToTerminalAsync($"\x1b[1;34m▶ Chain [{string.Join(" → ", _chain)}]\x1b[0m");

        try
        {
            var results = await invoker.RunChainAsync(_chain, context, progress, _cts.Token);

            await WriteToTerminalAsync("");
            await WriteToTerminalAsync("\x1b[2m─── Tóm tắt ──────────────────\x1b[0m");
            foreach (var r in results)
            {
                var color = r.Outcome switch
                {
                    AtomOutcome.Completed => "\x1b[32m",
                    AtomOutcome.Skipped => "\x1b[33m",
                    _ => "\x1b[31m",
                };
                await WriteToTerminalAsync($"  {color}{r.AtomId}: {r.Outcome}\x1b[0m");
            }
        }
        catch (OperationCanceledException)
        {
            await WriteToTerminalAsync("\x1b[33m[CANCELLED]\x1b[0m");
        }

        await RefreshBranchInfoAsync();
        BtnRun.IsEnabled = true;
        BtnRun.Content = "▶  RUN CHAIN";
    }

    // ═══════════════════════════════════════════════
    // TOP BAR ACTIONS
    // ═══════════════════════════════════════════════

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WinForms.FolderBrowserDialog
        {
            Description = "Chọn thư mục Git repository",
            UseDescriptionForTitle = true,
            SelectedPath = _workingDir,
        };

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            _workingDir = dialog.SelectedPath;
            TxtWorkingDir.Text = $"📁 {_workingDir}";
            RefreshBranchInfo();
            _ = WriteToTerminalAsync($"\x1b[2mThư mục: {_workingDir}\x1b[0m");
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
        MessageBox.Show("Save Preset — Coming soon!", "dev-git-atom", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}



