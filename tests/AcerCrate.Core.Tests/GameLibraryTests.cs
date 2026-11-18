using System.Text.Json;
using AcerCrate.Core.Settings;

namespace AcerCrate.Core.Tests;

public sealed class GameLibraryTests
{
    [Fact]
    public void A_new_user_starts_in_English_and_has_an_empty_game_library()
    {
        var settings = new UserSettings();
        Assert.Equal("en-US", settings.Ui.Language);
        Assert.Empty(settings.Games);
    }
    [Fact]
    public void Imports_a_steam_game_and_preserves_its_client_launch_uri()
    {
        var game = GameLibrary.SteamManifest("\"AppState\" { \"appid\" \"570\" \"name\" \"Dota 2\" }");
        Assert.NotNull(game);
        Assert.Equal("Dota 2", game.Name);
        Assert.Equal("steam://rungameid/570", game.LaunchUri);
        Assert.True(game.CanLaunch);
    }

    [Fact]
    public void Rejects_non_numeric_steam_identifiers() => Assert.Null(GameLibrary.SteamManifest("\"appid\" \"../../bad\" \"name\" \"bad\""));

    [Fact]
    public void Steam_library_paths_are_unescaped_and_deduplicated() =>
        Assert.Equal(new[] { @"D:\SteamLibrary" }, GameLibrary.SteamLibraries("\"path\" \"D:\\\\SteamLibrary\" \"path\" \"d:\\\\SteamLibrary\""));

    [Fact]
    public void Imports_epic_via_the_launcher_instead_of_bypassing_the_client()
    {
        var game = GameLibrary.EpicManifest("{\"AppName\":\"game id\",\"DisplayName\":\"Example\",\"InstallLocation\":\"D:\\\\Games\"}");
        Assert.NotNull(game);
        Assert.Contains("game%20id", game.LaunchUri);
        Assert.True(game.CanLaunch);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/System32/cmd.exe")]
    public void Unknown_launcher_schemes_are_not_executed(string uri) => Assert.False(GameLibrary.IsLauncherUri(uri));

    [Fact]
    public void Reimport_keeps_user_favorites_and_launch_history()
    {
        var saved = new GameEntry { Name = "My name", LaunchUri = "steam://rungameid/570", Favorite = true, LastPlayed = DateTimeOffset.UtcNow };
        var games = GameLibrary.Merge([saved], [GameLibrary.SteamManifest("\"appid\" \"570\" \"name\" \"Dota 2\"")!]);
        Assert.Same(saved, Assert.Single(games));
    }

    [Fact]
    public void Home_holds_three_pins_without_limiting_the_library()
    {
        var games = Enumerable.Range(1, 4)
            .Select(index => new GameEntry { Name = $"Game {index}", LaunchUri = $"steam://rungameid/{index}" })
            .ToList();
        var before = new UserSettings { Games = games };
        var pinned = games.Aggregate(before, GameLibrary.PinToHome);

        Assert.Same(games, pinned.Games);
        Assert.Equal(games.Take(3), GameLibrary.HomeGames(pinned));
        Assert.Same(pinned, GameLibrary.PinToHome(pinned, games[3]));
    }

    [Fact]
    public void Home_reuses_the_existing_executable_and_preserves_the_pin_during_serialization()
    {
        var game = new GameEntry { Name = "Existing title", Executable = @"D:\Games\Game.exe", Arguments = "-windowed" };
        var previous = new UserSettings { Games = [game], HomeGameIds = ["deleted"] };
        var updated = GameLibrary.PinToHome(previous, new GameEntry { Executable = @"d:\games\game.exe" });
        Assert.Same(updated, GameLibrary.PinToHome(updated, game));

        var json = JsonSerializer.Serialize(updated, SettingsJson.Options);
        var reloaded = JsonSerializer.Deserialize<UserSettings>(json, SettingsJson.Options)!;
        Assert.Equal(game, Assert.Single(GameLibrary.HomeGames(reloaded)));
        Assert.Equal(game, Assert.Single(reloaded.Games));
    }
}
