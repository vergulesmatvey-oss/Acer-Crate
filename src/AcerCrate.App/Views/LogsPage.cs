using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using AcerCrate.App.Controls;
using AcerCrate.App.Services;
using static AcerCrate.App.Controls.CrateUi;

namespace AcerCrate.App.Views;

public sealed partial class LogsPage : Page
{
    private readonly ComboBox origin = new() { Width = 200 };
    private readonly ComboBox severity = new() { Width = 160 };
    private readonly TextBox search = new() { PlaceholderText = L("Search log text", "Поиск по журналу"), MinWidth = 180 };
    private readonly TextBox output = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), FontSize = 12, MinHeight = 300, MaxHeight = 620, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock status = Text("", 12, .65);
    private string content = "";
    private bool loading;

    public LogsPage()
    {
        InitializeComponent();
        var body = Body();
        body.Children.Add(new PageHeading { Title = L("Logs", "Журнал"), Description = L("Mode changes, launch events and troubleshooting", "Смена режимов, запуски и диагностика"), Glyph = "\uE9D9" });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        actions.Children.Add(Button(L("Refresh", "Обновить"), async (_, _) => await ReadAsync(), true));
        actions.Children.Add(Button(L("Open folder", "Открыть папку"), (_, _) =>
        {
            try
            {
                var folder = SelectedDirectory();
                if (!Directory.Exists(folder)) { status.Text = L("No events recorded yet.", "Событий пока нет."); return; }
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true })?.Dispose();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { status.Text = e.Message; }
        }));
        actions.Children.Add(Button(L("Export visible log", "Экспортировать журнал"), async (_, _) => await ExportAsync()));
        body.Children.Add(actions);
        origin.ItemsSource = new[] { L("Application", "Приложение"), L("Control service", "Служба управления") }; origin.SelectedIndex = 0;
        severity.ItemsSource = new[] { L("All levels", "Все уровни"), L("Warnings / errors", "Ошибки / предупреждения") }; severity.SelectedIndex = 0;
        var filters = new Grid { ColumnSpacing = 10 };
        filters.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); filters.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); filters.ColumnDefinitions.Add(new ColumnDefinition());
        filters.Children.Add(origin); Grid.SetColumn(severity, 1); filters.Children.Add(severity); Grid.SetColumn(search, 2); filters.Children.Add(search);
        body.Children.Add(filters); body.Children.Add(status);
        body.Children.Add(Card(L("Activity", "События"), L("Logs stay on this computer. Review exported files before sharing.", "Журналы хранятся на этом компьютере. Проверьте файл перед отправкой."), output));
        origin.SelectionChanged += async (_, _) => await ReadAsync(); severity.SelectionChanged += (_, _) => Filter(); search.TextChanged += (_, _) => Filter();
        Content = new ScrollView { Content = body };
        Loaded += async (_, _) => await ReadAsync();
    }
    private string SelectedDirectory() => origin.SelectedIndex == 1 ? AppLogFiles.ServiceDirectory : AppLogFiles.ClientDirectory;
    private async Task ReadAsync()
    {
        if (loading) return; loading = true;
        try
        {
            var folder = SelectedDirectory();
            content = await Task.Run(() =>
            {
                if (!Directory.Exists(folder)) return "";
                var file = Directory.EnumerateFiles(folder, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
                if (file is null) return "";
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var skipped = stream.Length > 256 * 1024;
                if (skipped) stream.Seek(-256 * 1024, SeekOrigin.End);
                using var reader = new StreamReader(stream, Encoding.UTF8, true);
                if (skipped) reader.ReadLine();
                return reader.ReadToEnd();
            });
            status.Text = string.IsNullOrEmpty(content) ? L("No events recorded yet.", "Событий пока нет.") : SelectedDirectory();
            Filter();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { status.Text = e.Message; }
        finally { loading = false; }
    }
    private void Filter()
    {
        var query = search.Text.Trim();
        var entries = content.Split('\n').Where(line => query.Length == 0 || line.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        if (severity.SelectedIndex == 1) entries = entries.Where(line => line.Contains("[WRN]", StringComparison.Ordinal) || line.Contains("[ERR]", StringComparison.Ordinal) || line.Contains("[FTL]", StringComparison.Ordinal));
        output.Text = string.Join('\n', entries);
    }
    private async Task ExportAsync()
    {
        var window = App.Current.Services.GetRequiredService<MainWindow>();
        var picker = new FileSavePicker(window.AppWindow.Id) { SuggestedFileName = "AcerCrate-log.txt" };
        picker.FileTypeChoices.Add("Text", new List<string> { ".txt" });
        try
        {
            var file = await picker.PickSaveFileAsync();
            if (file is not null) { await File.WriteAllTextAsync(file.Path, output.Text); status.Text = L("Exported", "Экспортировано"); }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException) { status.Text = e.Message; }
    }
}
