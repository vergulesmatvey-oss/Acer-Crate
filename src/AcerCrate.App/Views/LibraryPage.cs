using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using AcerCrate.App.Controls;
using AcerCrate.App.Services;
using AcerCrate.Core.Settings;
using static AcerCrate.App.Controls.CrateUi;

namespace AcerCrate.App.Views;

public sealed partial class LibraryPage : Page
{
    private readonly GameLibraryService library = App.Current.Services.GetRequiredService<GameLibraryService>();
    private readonly SettingsService settings = App.Current.Services.GetRequiredService<SettingsService>();
    private readonly TileGrid cards = new() { MinColumnWidth = 280, MaxColumns = 4, Spacing = 16 };
    private readonly TextBox search = new() { PlaceholderText = L("Search games", "Поиск игр"), MinWidth = 200, MaxWidth = 380, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock status = Text("", 13, .65);
    private readonly ComboBox order = new() { Width = 180 };
    private bool scanning;
    private int shown = 48;
    private readonly Button more;

    public LibraryPage()
    {
        InitializeComponent();
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        var body = Body();
        body.Children.Add(new PageHeading { Title = L("Game library", "Игровая библиотека"), Description = L("Your games, launchers and recent sessions", "Игры, лаунчеры и последние запуски"), Glyph = "\uE7FC" });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        actions.Children.Add(Button(L("Add game", "Добавить игру"), async (_, _) => await AddAsync(), true));
        actions.Children.Add(Button(L("Find installed games", "Найти установленные игры"), async (_, _) => await DiscoverAsync()));
        body.Children.Add(actions);
        var automatic = new ToggleSwitch { Header = L("Detect Steam / Epic games at startup", "Автопоиск игр Steam / Epic при запуске"), IsOn = settings.Current.Ui.AutoDiscoverGames };
        automatic.Toggled += async (_, _) =>
        {
            settings.Update(s => s with { Ui = s.Ui with { AutoDiscoverGames = automatic.IsOn } });
            if (automatic.IsOn) await DiscoverAsync();
        };
        body.Children.Add(automatic);
        var toolbar = new Grid { ColumnSpacing = 16 };
        toolbar.ColumnDefinitions.Add(new ColumnDefinition()); toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.Children.Add(search);
        order.ItemsSource = new[] { L("Recently played", "Последние запуски"), L("Name", "Название"), L("Favorites", "Избранное") };
        order.SelectedIndex = 0; Grid.SetColumn(order, 1); toolbar.Children.Add(order); body.Children.Add(toolbar);
        search.TextChanged += (_, _) => { shown = 48; Render(); }; order.SelectionChanged += (_, _) => { shown = 48; Render(); };
        body.Children.Add(status); body.Children.Add(cards);
        more = Button(L("Load more", "Показать ещё"), (_, _) => { shown += 48; Render(); });
        body.Children.Add(more);
        Content = new ScrollView { Content = body };
        Loaded += (_, _) => { settings.Changed += Changed; Render(); };
        Unloaded += (_, _) => settings.Changed -= Changed;
    }

    private void Changed(UserSettings _) => DispatcherQueue.TryEnqueue(() => { if (IsLoaded && App.Current.IsWindowVisible) Render(); });
    private void Render()
    {
        cards.Children.Clear();
        var games = library.Games.Where(g => g.Name.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase));
        games = order.SelectedIndex switch
        {
            1 => games.OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase),
            2 => games.Where(g => g.Favorite).OrderBy(g => g.Name),
            _ => games.OrderByDescending(g => g.Favorite).ThenByDescending(g => g.LastPlayed).ThenBy(g => g.Name)
        };
        var visible = games.ToArray();
        if (!scanning) status.Text = library.Games.Count == 0 ? L("Add an executable or import installed Steam and Epic games.", "Добавьте исполняемый файл или импортируйте установленные игры Steam и Epic.") : $"{visible.Length} / {library.Games.Count}";
        more.Visibility = visible.Length > shown ? Visibility.Visible : Visibility.Collapsed;
        foreach (var game in visible.Take(shown))
        {
            var panel = new StackPanel { Spacing = 14 };
            var visual = new Border { Height = 94, CornerRadius = new CornerRadius(10), Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CrateSurface"], Child = new FontIcon { Glyph = "\uE7FC", FontSize = 38, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CrateAccent"] } };
            panel.Children.Add(visual);
            var name = Text(game.Name, 19); name.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; panel.Children.Add(name);
            panel.Children.Add(Text(game.Source, 12, .6));
            panel.Children.Add(Text(game.LastPlayed is { } last ? L("Last played: ", "Последний запуск: ") + last.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture) : L("Not launched yet", "Ещё не запускалась"), 12, .6));
            panel.Children.Add(Button(L("Play", "Играть"), (_, _) =>
            {
                try { library.Launch(game); } catch (Exception e) { status.Text = e.Message; }
            }, true));
            var secondary = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            secondary.Children.Add(Button(game.Favorite ? "★" : "☆", (_, _) => library.Favorite(game.Id)));
            secondary.Children.Add(Button(L("Remove", "Убрать"), (_, _) => library.Remove(game.Id)));
            panel.Children.Add(secondary);
            cards.Children.Add(new Border { Style = (Style)Application.Current.Resources["CardStyle"], Child = panel });
        }
    }

    private async Task AddAsync()
    {
        var window = App.Current.Services.GetRequiredService<MainWindow>();
        var picker = new FileOpenPicker(window.AppWindow.Id);
        picker.FileTypeFilter.Add(".exe"); picker.FileTypeFilter.Add(".lnk");
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;
        var name = new TextBox { Header = L("Name", "Название"), Text = Path.GetFileNameWithoutExtension(file.Path), MaxLength = 120 };
        var arguments = new TextBox { Header = L("Launch arguments (optional)", "Параметры запуска (необязательно)") };
        var fields = new StackPanel { Spacing = 12 }; fields.Children.Add(name); fields.Children.Add(arguments);
        var dialog = new ContentDialog { XamlRoot = XamlRoot, RequestedTheme = ActualTheme, Title = L("Add game", "Добавить игру"), Content = fields, PrimaryButtonText = L("Add", "Добавить"), CloseButtonText = L("Cancel", "Отмена"), DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try { library.Add(new GameEntry { Name = string.IsNullOrWhiteSpace(name.Text) ? Path.GetFileNameWithoutExtension(file.Path) : name.Text.Trim(), Executable = file.Path, Arguments = arguments.Text }); }
        catch (Exception e) { status.Text = e.Message; }
    }

    private async Task DiscoverAsync()
    {
        if (scanning) return;
        scanning = true; status.Text = L("Searching installed launchers…", "Поиск установленных лаунчеров…");
        try { var count = await library.DiscoverAsync(); scanning = false; Render(); status.Text = L("New games: ", "Новых игр: ") + count; }
        catch (Exception e) { status.Text = e.Message; }
        finally { scanning = false; }
    }
}
