using System.Windows;
using Wpf.Ui.Controls;

namespace PbePro.Views;

public sealed class RandomBuildRequest
{
    public int Count { get; init; }
    public string? Category { get; init; }
    public int? Points { get; init; }
    public bool Replace { get; init; }
}

public partial class RandomBuildDialog : FluentWindow
{
    public const string AllCategories = "All categories";
    public const string NoCategory = "(No category)";
    public const string AllPoints = "All points";

    public RandomBuildDialog(IReadOnlyList<string> categoryOptions, IReadOnlyList<string> pointsOptions)
    {
        InitializeComponent();
        CategoryBox.ItemsSource = categoryOptions;
        CategoryBox.SelectedItem = categoryOptions.FirstOrDefault() ?? AllCategories;
        PointsBox.ItemsSource = pointsOptions;
        PointsBox.SelectedItem = pointsOptions.FirstOrDefault() ?? AllPoints;
    }

    public RandomBuildRequest? Result { get; private set; }

    public static RandomBuildRequest? Prompt(
        IReadOnlyList<string> categoryOptions, IReadOnlyList<string> pointsOptions)
    {
        var dialog = new RandomBuildDialog(categoryOptions, pointsOptions);
        if (Application.Current?.MainWindow is { } owner)
            dialog.Owner = owner;

        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    private void OnBuild(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CountBox.Text.Trim(), out var count) || count < 1)
        {
            ErrorText.Text = "Enter how many questions to pick, as a number of 1 or more.";
            return;
        }

        var category = CategoryBox.SelectedItem as string;
        string? categoryFilter = category switch
        {
            null or AllCategories => null,
            NoCategory => "",
            _ => category
        };

        var points = PointsBox.SelectedItem as string;
        int? pointsFilter = points is null or AllPoints ? null : int.Parse(points);

        Result = new RandomBuildRequest
        {
            Count = count,
            Category = categoryFilter,
            Points = pointsFilter,
            Replace = ReplaceRadio.IsChecked == true
        };
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
