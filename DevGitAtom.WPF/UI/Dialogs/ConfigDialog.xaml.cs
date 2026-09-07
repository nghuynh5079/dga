using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevGitAtom.Contracts;
using DevGitAtom.Engine;
using DevGitAtom.GitAtoms;
using DevGitAtom.GitAtoms.Atoms;

namespace DevGitAtom.WPF.UI.Dialogs;

public partial class ConfigDialog : Window
{
    private readonly ChainStep _step;
    private readonly Dictionary<string, System.Windows.Controls.TextBox> _inputs = new();

    public ConfigDialog(IAtom atom, ChainStep step)
    {
        InitializeComponent();
        _step = step;
        TxtTitle.Text = $"Cấu hình tham số: {atom.DisplayName}";

        foreach (var param in atom.Parameters)
        {
            var label = new TextBlock
            {
                Text = param.DisplayName,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8b, 0x94, 0x9e)),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var value = step.Parameters.TryGetValue(param.Key, out var v) ? v : param.DefaultValue;
            var textBox = new System.Windows.Controls.TextBox
            {
                Text = value,
                Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1b, 0x22)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xe6, 0xed, 0xf3)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3d)),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 12)
            };

            _inputs[param.Key] = textBox;
            ParamsPanel.Children.Add(label);
            ParamsPanel.Children.Add(textBox);
        }
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        foreach (var kvp in _inputs)
        {
            _step.Parameters[kvp.Key] = kvp.Value.Text;
        }
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}



