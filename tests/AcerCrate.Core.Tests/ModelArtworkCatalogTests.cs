using System.Text.Json;
using AcerCrate.Core.Hardware;

namespace AcerCrate.Core.Tests;

public sealed class ModelArtworkCatalogTests
{
    [Theory]
    [InlineData("Nitro AN515-58", "AN515-58")]
    [InlineData("Predator PHN16S-71-981M", "PHN16S-71")]
    [InlineData("PT314-52s", "PT314-52S")]
    [InlineData("PT14-52T", "PT14-52T")]
    [InlineData("PH3D15-71", "PH3D15-71")]
    [InlineData("ANV16S 41", "ANV16S-41")]
    public void Recognizes_model_codes_and_suffixes(string model, string code) => Assert.Equal(code, ModelArtworkCatalog.Code(model));

    [Fact]
    public void Exact_model_wins_over_a_family_image()
    {
        ModelArtworkEntry[] entries = [new() { File = "family.png", Prefixes = ["PHN16"] }, new() { File = "exact.png", Models = ["PHN16-73"] }];
        var match = ModelArtworkCatalog.Find("Predator PHN16-73", entries);
        Assert.True(match!.IsExact); Assert.Equal("exact.png", match.Entry.File);
    }

    [Fact]
    public void Series_fallback_does_not_claim_an_exact_match()
    {
        ModelArtworkEntry[] entries = [new() { Prefixes = ["ANV16"] }];
        Assert.False(ModelArtworkCatalog.Find("ANV16-72", entries)!.IsExact);
        Assert.Null(ModelArtworkCatalog.Find("ANV16S-41", entries));
        Assert.Null(ModelArtworkCatalog.Find("Unknown laptop", entries));
    }

    [Fact]
    public void Bundled_catalog_contains_real_image_files_and_unique_model_codes()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AcerCrate.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, "src", "AcerCrate.App", "Assets", "Models");
        var entries = JsonSerializer.Deserialize<ModelArtworkEntry[]>(File.ReadAllText(Path.Combine(folder, "catalog.json")))!;
        Assert.True(entries.Length >= 28);
        var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            Assert.Equal(Path.GetFileName(entry.File), entry.File);
            foreach (var code in entry.Models) Assert.True(models.Add(code), code);
            var data = File.ReadAllBytes(Path.Combine(folder, entry.File));
            Assert.InRange(data.Length, 1000, 4 * 1024 * 1024);
            var png = data.AsSpan(0, 4).SequenceEqual(new byte[] { 137, 80, 78, 71 });
            var webp = System.Text.Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(data, 8, 4) == "WEBP";
            var jpeg = data[0] == 255 && data[1] == 216 && data[2] == 255;
            Assert.True(png || webp || jpeg, entry.File);
        }
    }
}
