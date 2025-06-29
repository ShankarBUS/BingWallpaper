using System.Text.Json.Serialization;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BingWallpaper.Models;

public class BingWallpaperImage
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("urlbase")]
    public string? UrlBase { get; set; }

    [JsonPropertyName("copyright")]
    public string? Copyright { get; set; }

    [JsonPropertyName("copyrightlink")]
    public string? CopyrightLink { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonIgnore]
    public string? Caption { get; set; }

    [JsonIgnore]
    public BitmapImage? Thumbnail { get; set; }

    [JsonIgnore]
    public StorageFile? ImageFile { get; set; }
}
