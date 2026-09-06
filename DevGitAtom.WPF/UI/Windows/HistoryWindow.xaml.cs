using System.Windows;
using DevGitAtom.Engine;

namespace DevGitAtom.WPF.UI.Windows;

public partial class HistoryWindow : Window
{
    private readonly HistoryManager _historyManager;

    public HistoryWindow()
    {
        InitializeComponent();
        _historyManager = new HistoryManager();
        Loaded += HistoryWindow_Loaded;
    }

    private void HistoryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var records = _historyManager.GetHistory();
        LvHistory.ItemsSource = records;
    }
}
