using System.Windows;
using System.Windows.Controls;

namespace DevGitAtom.WPF.UI.Components;

public partial class AtomTitleBar : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(AtomTitleBar), new PropertyMetadata("DevGitAtom"));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty CenterContentProperty =
        DependencyProperty.Register("CenterContent", typeof(object), typeof(AtomTitleBar), new PropertyMetadata(null));

    public object CenterContent
    {
        get => GetValue(CenterContentProperty);
        set => SetValue(CenterContentProperty, value);
    }

    public AtomTitleBar()
    {
        InitializeComponent();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null) window.WindowState = WindowState.Minimized;
    }

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
                BtnMaximize.Content = "☐";
            }
            else
            {
                window.WindowState = WindowState.Maximized;
                BtnMaximize.Content = "❐";
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null) window.Close();
    }
}
