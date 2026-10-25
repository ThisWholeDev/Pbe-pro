using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace PbePro.Views;

public partial class PromptDialog : FluentWindow
{
    public PromptDialog(string title, string message, string initialValue, string confirmText)
    {
        InitializeComponent();
        Title = title;
        DialogTitleBar.Title = title;
        MessageText.Text = message;
        ValueBox.Text = initialValue;
        ConfirmButton.Content = confirmText;

        Loaded += (_, _) =>
        {
            ValueBox.Focus();
            ValueBox.SelectAll();
        };
    }

    public string? Result { get; private set; }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        var value = ValueBox.Text.Trim();
        if (value.Length == 0)
            return;

        Result = value;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            OnConfirm(this, new RoutedEventArgs());
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            DialogResult = false;
        }

        base.OnPreviewKeyDown(e);
    }
}
