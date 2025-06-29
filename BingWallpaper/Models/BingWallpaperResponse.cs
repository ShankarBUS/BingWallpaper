using System.Text.Json.Serialization;

namespace BingWallpaper.Models;

public class BingWallpaperResponse
{
    [JsonPropertyName("images")]
    public BingWallpaperImage[]? Images { get; set; }
}
