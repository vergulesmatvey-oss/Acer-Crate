using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http.Headers;
using AcerCrate.Core.Settings;
using AcerCrate.Core.Hardware;
using System.Text.Json;

namespace AcerCrate.App.Services;

public static partial class ModelArtwork
{
    private static readonly HttpClient http = CreateClient();
    public static IReadOnlyList<ModelArtworkEntry> Entries { get; } = LoadCatalog();
    public static ModelArtworkMatch? Match(string? model) => ModelArtworkCatalog.Find(model, Entries);
    private static IReadOnlyList<ModelArtworkEntry> LoadCatalog()
    {
        try { return JsonSerializer.Deserialize<ModelArtworkEntry[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "catalog.json"))) ?? []; }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return []; }
    }
    public static string? Resolve(UiSettings settings, string? detectedModel)
    {
        if (!string.IsNullOrWhiteSpace(settings.CustomLaptopImage))
            return settings.CustomLaptopImage;
        return Catalog(settings.ArtworkModelQuery ?? detectedModel);
    }

    public static string? Catalog(string? model)
    {
        if (Match(model) is not { } match || Path.GetFileName(match.Entry.File) != match.Entry.File) return null;
        var file = Path.Combine(AppContext.BaseDirectory, "Assets", "Models", match.Entry.File);
        return File.Exists(file) ? file : null;
    }

    public static async Task<string?> ResolveAsync(UiSettings settings, string? detectedModel)
    {
        if (Resolve(settings, detectedModel) is { } local) return local;
        var code = ModelArtworkCatalog.Code(settings.ArtworkModelQuery ?? detectedModel);
        if (code is null) return null;
        var cache = Path.Combine(AppLogFiles.ClientDirectory, "Artwork", code + ".img");
        if (File.Exists(cache)) return cache;
        if (!settings.DownloadModelArtwork) return null;
        try
        {
            // Only a public model code is sent; serial numbers and diagnostics are not used.
            var search = new Uri("https://store.acer.com/en-gb/catalogsearch/result/?q=" + Uri.EscapeDataString(code));
            var html = await ReadHtmlAsync(search);
            var slug = code.ToLowerInvariant();
            Uri? product = null;
            foreach (Match match in ProductLinks().Matches(html))
            {
                if (!Uri.TryCreate(search, WebUtility.HtmlDecode(match.Groups[1].Value), out var candidate)) continue;
                if (candidate.Host == "store.acer.com" && candidate.AbsolutePath.Contains(slug, StringComparison.OrdinalIgnoreCase) && !candidate.AbsolutePath.Contains("catalogsearch", StringComparison.OrdinalIgnoreCase)) { product = candidate; break; }
            }
            if (product is null) return null;
            html = await ReadHtmlAsync(product);
            var image = OgImage().Match(html);
            if (!image.Success) return null;
            if (!Uri.TryCreate(WebUtility.HtmlDecode(image.Groups[1].Value), UriKind.Absolute, out var imageUri) || imageUri.Scheme != "https" ||
                !(imageUri.Host == "acer.com" || imageUri.Host.EndsWith(".acer.com", StringComparison.OrdinalIgnoreCase))) return null;
            using var response = await http.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true || response.Content.Headers.ContentLength > 8 * 1024 * 1024) return null;
            using var data = new MemoryStream();
            await using var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[16384]; int read;
            while ((read = await stream.ReadAsync(buffer)) > 0) { if (data.Length + read > 8 * 1024 * 1024) return null; await data.WriteAsync(buffer.AsMemory(0, read)); }
            var bytes = data.ToArray();
            // Decode before caching, so HTML or a broken file cannot become a model image.
            if (await AcerCrate.App.Helpers.Pictures.FromBytesAsync(bytes, 520) is null) return null;
            Directory.CreateDirectory(Path.GetDirectoryName(cache)!);
            await File.WriteAllBytesAsync(cache, bytes);
            return cache;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException) { return null; }
    }

    private static async Task<string> ReadHtmlAsync(Uri uri)
    {
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        var buffer = new char[1024 * 1024];
        var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
        return new string(buffer, 0, read);
    }
    private static HttpClient CreateClient()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AcerCrate", "1.1.0"));
        return client;
    }
    [GeneratedRegex("href=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProductLinks();
    [GeneratedRegex("<meta[^>]*property=[\"']og:image[\"'][^>]*content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OgImage();

}
