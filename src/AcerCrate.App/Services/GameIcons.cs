using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using AcerCrate.Core.Settings;

namespace AcerCrate.App.Services;

internal static class GameIcons
{
    private const string SteamLaunch = "steam://rungameid/";

    public static async Task<BitmapImage?> ReadAsync(GameEntry game)
    {
        var icon = await ReadExecutableAsync(game.Executable);
        if (icon is not null) return icon;
        if (game.LaunchUri is not { } uri || !uri.StartsWith(SteamLaunch, StringComparison.OrdinalIgnoreCase) ||
            !uint.TryParse(uri.AsSpan(SteamLaunch.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var appId))
            return null;

        (string Path, bool Square)[] images;
        try { images = await Task.Run(() => SteamImages(appId)); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { return null; }

        foreach (var (path, square) in images)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                using var stream = await file.OpenReadAsync();
                if (stream.Size is 0 or > 5_000_000) continue;
                if (square)
                {
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    if (decoder.PixelWidth != decoder.PixelHeight || decoder.PixelWidth is < 16 or > 256) continue;
                    stream.Seek(0);
                }
                var bitmap = new BitmapImage { DecodePixelWidth = 96 };
                await bitmap.SetSourceAsync(stream);
                return bitmap;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or COMException or ArgumentException) { }
        }
        return null;
    }

    private static async Task<BitmapImage?> ReadExecutableAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
            path.StartsWith(@"\\", StringComparison.Ordinal)) return null;
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 64);
            if (thumbnail is null || thumbnail.Size == 0) return null;
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(thumbnail);
            return bitmap;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or COMException or ArgumentException) { return null; }
    }

    private static (string Path, bool Square)[] SteamImages(uint appId)
    {
        var images = new List<(string, bool)>();
        var id = appId.ToString(CultureInfo.InvariantCulture);
        foreach (var root in GameLibraryService.SteamInstallations())
        {
            var cache = Path.Combine(root, "appcache", "librarycache");
            var oldIcon = Path.Combine(cache, id + "_icon.jpg");
            if (File.Exists(oldIcon)) images.Add((oldIcon, true));

            var folder = Path.Combine(cache, id);
            if (!Directory.Exists(folder)) continue;
            foreach (var file in Directory.EnumerateFiles(folder, "*.jpg").Where(IsHashedAsset).Take(12))
                images.Add((file, true));

            var logo = Path.Combine(folder, "logo.png");
            if (File.Exists(logo)) images.Add((logo, false));
        }
        return images.ToArray();
    }

    private static bool IsHashedAsset(string file)
    {
        var name = Path.GetFileNameWithoutExtension(file);
        return name.Length == 40 && name.All(Uri.IsHexDigit);
    }
}
