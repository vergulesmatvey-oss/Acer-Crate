using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using AcerCrate.App.Services;
using AcerCrate.Core.Settings;

namespace AcerCrate.App.Controls;

public sealed partial class HomeGames : UserControl
{
    private readonly SettingsService settings = App.Current.Services.GetRequiredService<SettingsService>();
    private readonly GameLibraryService library = App.Current.Services.GetRequiredService<GameLibraryService>();
    private IReadOnlyList<GameEntry> shown = [];
    private bool rendered;
    private bool picking;

    public HomeGames()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            settings.Changed += OnSettingsChanged;
            App.Current.WindowVisibilityChanged += OnWindowVisibilityChanged;
            Render();
        };
        Unloaded += (_, _) =>
        {
            settings.Changed -= OnSettingsChanged;
            App.Current.WindowVisibilityChanged -= OnWindowVisibilityChanged;
        };
    }

    private void OnSettingsChanged(UserSettings _) => DispatcherQueue.TryEnqueue(() =>
    {
        if (IsLoaded) OnWindowVisibilityChanged();
    });

    private void OnWindowVisibilityChanged()
    {
        if (App.Current.IsWindowVisible) Render();
    }

    private void Render()
    {
        var games = GameLibrary.HomeGames(settings.Current);
        if (rendered && shown.Select(g => (g.Id, g.Name, g.Executable, g.Source))
            .SequenceEqual(games.Select(g => (g.Id, g.Name, g.Executable, g.Source))))
            return;
        shown = games;
        rendered = true;
        Count.Text = $"{games.Count} / {GameLibrary.HomeLimit}";
        Tiles.Children.Clear();
        foreach (var game in games) Tiles.Children.Add(GameCard(game));
        if (games.Count < GameLibrary.HomeLimit) Tiles.Children.Add(AddCard());
    }

    private Button AddCard()
    {
        var title = T("Добавить игру");
        var body = new StackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        body.Children.Add(Mark("\uE710", 22));
        body.Children.Add(new TextBlock { Text = title, FontSize = 12, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
        var card = Tile(body);
        AutomationProperties.SetName(card, title);
        card.Click += AddGame;
        return card;
    }

    private Grid GameCard(GameEntry game)
    {
        var preview = new Grid { Height = 42, Margin = new Thickness(0, 2, 0, 1) };
        var mark = Mark(game.LaunchUri is null ? "\uE8A5" : "\uE7FC", 27);
        preview.Children.Add(mark);
        _ = LoadThumbnailAsync(preview, mark, game);

        var body = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        body.Children.Add(preview);
        body.Children.Add(new TextBlock
        {
            Text = game.Name, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis,
        });
        body.Children.Add(new TextBlock { Text = game.Source == "Manual" ? T("Приложение") : game.Source,
            FontSize = 11, Opacity = .72, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });

        var play = Tile(body);
        var playTitle = F("HomeLaunchGame", game.Name);
        AutomationProperties.SetName(play, playTitle);
        ToolTipService.SetToolTip(play, playTitle);
        play.Click += (_, _) => Launch(game);

        var remove = new MenuFlyoutItem
        {
            Text = T("Убрать с главной"),
            Icon = new FontIcon { Glyph = "\uE711" },
        };
        remove.Click += (_, _) => { SetStatus(null); library.UnpinFromHome(game.Id); };
        var flyout = new MenuFlyout();
        flyout.Items.Add(remove);
        var actions = new Button
        {
            Content = new FontIcon { Glyph = "\uE712", FontSize = 13 },
            Padding = new Thickness(4),
            MinWidth = 26, MinHeight = 24, Margin = new Thickness(0, 4, 4, 0),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
            Flyout = flyout,
        };
        AutomationProperties.SetName(actions, F("Действия для {0}", game.Name));
        var card = new Grid();
        card.Children.Add(play);
        card.Children.Add(actions);
        return card;
    }

    private FontIcon Mark(string glyph, double size) => new()
    {
        Glyph = glyph, FontSize = size, Style = (Style)Resources["TileMark"],
    };

    private Button Tile(UIElement content) => new()
    {
        Content = content, Style = (Style)Resources["TileButton"], MinHeight = 98,
    };

    private async void AddGame(object sender, RoutedEventArgs args)
    {
        if (picking || GameLibrary.HomeGames(settings.Current).Count >= GameLibrary.HomeLimit) return;
        picking = true;
        SetStatus(null);
        try
        {
            var game = await ChooseGameAsync();
            if (game is not null && !library.PinToHome(game)) SetStatus(T("Не удалось закрепить игру."));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            SetStatus(T("Не удалось закрепить игру."));
        }
        finally
        {
            picking = false;
        }
    }

    private async Task<GameEntry?> ChooseGameAsync()
    {
        var pinned = GameLibrary.HomeGames(settings.Current).Select(game => game.Id).ToHashSet(StringComparer.Ordinal);
        var available = library.Games.Where(game => game.CanLaunch && !pinned.Contains(game.Id))
            .OrderBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
        var games = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single, MaxHeight = 288,
            DisplayMemberPath = nameof(GameEntry.Name), ItemsSource = available,
            Visibility = available.Length == 0 ? Visibility.Collapsed : Visibility.Visible,
        };
        var search = new TextBox { PlaceholderText = T("Поиск по библиотеке"),
            Visibility = available.Length == 0 ? Visibility.Collapsed : Visibility.Visible };
        search.TextChanged += (_, _) => games.ItemsSource = available.Where(game =>
            game.Name.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();
        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(search);
        content.Children.Add(games);
        if (available.Length == 0) content.Children.Add(CrateUi.Text(T("Выберите файл приложения или ярлык."), 13));
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot, RequestedTheme = ActualTheme, Title = T("Добавить игру"), Content = content,
            PrimaryButtonText = T("Закрепить"), SecondaryButtonText = T("Выбрать файл…"),
            CloseButtonText = T("Отмена"), IsPrimaryButtonEnabled = false,
        };
        games.SelectionChanged += (_, _) => dialog.IsPrimaryButtonEnabled = games.SelectedItem is GameEntry;
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary) return games.SelectedItem as GameEntry;
        if (result != ContentDialogResult.Secondary) return null;
        var window = App.Current.Services.GetRequiredService<MainWindow>();
        var path = await GameLibraryService.PickExecutableAsync(window.AppWindow.Id);
        return path is null ? null : new GameEntry { Name = Path.GetFileNameWithoutExtension(path), Executable = path };
    }

    private static async Task LoadThumbnailAsync(Grid preview, UIElement fallback, GameEntry game)
    {
        var bitmap = await GameIcons.ReadAsync(game);
        if (bitmap is null) return;
        preview.Children.Add(new Image { Source = bitmap, Width = 40, Height = 40 });
        fallback.Visibility = Visibility.Collapsed;
    }

    private void Launch(GameEntry game)
    {
        if (App.Current.Options.Preview) { SetStatus(T("Просмотр интерфейса")); return; }
        SetStatus(null);
        try { library.Launch(game); }
        catch (Exception error) when (error is IOException or Win32Exception or InvalidOperationException or ArgumentException)
        {
            SetStatus(F("Не удалось запустить {0}.", game.Name));
        }
    }

    private void SetStatus(string? text)
    {
        Status.Text = text ?? "";
        Status.Visibility = text is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OpenLibrary(object sender, RoutedEventArgs args) =>
        App.Current.Services.GetRequiredService<NavigationService>().Navigate("library");
}
