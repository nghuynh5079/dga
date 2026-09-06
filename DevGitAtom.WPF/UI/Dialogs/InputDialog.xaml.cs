using System.Windows;

namespace DevGitAtom.WPF.UI.Dialogs;

public partial class InputDialog : Window
{
    public string InputText => TxtInput.Text.Trim();

    public InputDialog(string defaultText = "")
    {
        InitializeComponent();
        TxtInput.Text = defaultText;
        TxtInput.Focus();
        TxtInput.SelectAll();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            MessageBox.Show("Vui lòng nhập tên preset", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}


