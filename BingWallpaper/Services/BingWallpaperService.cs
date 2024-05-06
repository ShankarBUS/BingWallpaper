using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json.Serialization;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;
using Windows.System.UserProfile;
using System.Net.NetworkInformation;

namespace BingWallpaper.Services;

public enum Resolution
{
    UHD,
    High,
    Low
}

public enum Orientation
{
    Landscape,
    Portrait
}

public class BingWallpaperService
{
    public const string BingUrl = "https://www.bing.com";
    public const string BingWallpaperUrl = "https://www.bing.com/HPImageArchive.aspx";

    static BingWallpaperService()
    {
#if ANDROID
        PreferredResolution = Resolution.High;
        PreferredOrientation = Orientation.Portrait;
#else
        PreferredResolution = Resolution.UHD;
        PreferredOrientation = Orientation.Landscape;
#endif
    }

    public static Resolution PreferredResolution { get; set; }

    public static Orientation PreferredOrientation { get; set; }

    public static ObservableCollection<BingWallpaperImage> Images { get; private set; } = [];

    private static StorageFolder localCacheFolder;

    public static async Task FetchImagesAsync()
    {
        try
        {
            localCacheFolder = ApplicationData.Current.LocalFolder; // We can't set the wallpaper on Windows if the image is in LocalCacheFolder?
            string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
            string json = string.Empty;
            StorageFile? jsonFile = null;
            if (await localCacheFolder.TryGetItemAsync(fileName) is StorageFile file)
            {
                json = await FileIO.ReadTextAsync(file); // Read JSON from cache
                jsonFile = file;
            }
            else if (NetworkInterface.GetIsNetworkAvailable())
            {
                var query = HttpUtility.ParseQueryString(string.Empty);
                query.Add("format", "js");
                query.Add("idx", "0");
                query.Add("n", "8"); // Number of Images to get
                string newUrl = BingWallpaperUrl + "?" + query?.ToString();
                using HttpClient client = new();
                HttpResponseMessage response = await client.GetAsync(newUrl);
                if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    json = await response.Content.ReadAsStringAsync();
                    // Cache the JSON
                    jsonFile = await localCacheFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(jsonFile, json);
                }
            }

            if (!string.IsNullOrEmpty(json) && !string.IsNullOrWhiteSpace(json))
            {
                var bingResponse = JsonSerializer.Deserialize<BingWallpaperResponse>(json);
                if (bingResponse?.Images != null && bingResponse?.Images?.Length > 0)
                {
                    Images.Clear();
                    foreach (BingWallpaperImage image in bingResponse.Images)
                    {
                        await LoadImageDataAsync(image);
                        Images.Add(image);
                    }
                }
            }

            await ClearUnusedCacheFilesAsync(jsonFile);
        }
        catch { }
    }

    private static async Task ClearUnusedCacheFilesAsync(StorageFile? jsonFile)
    {
        try
        {
            List<StorageFile> neededFiles = [];
            if (jsonFile != null) neededFiles.Add(jsonFile);
            foreach (BingWallpaperImage image in Images)
            {
                if (image.ImageFile != null) neededFiles.Add(image.ImageFile);
            }

            IReadOnlyList<StorageFile> files = await localCacheFolder.GetFilesAsync();
            foreach (StorageFile file in files)
            {
                if (!neededFiles.Any(x => x.Path == file.Path))
                {
                    await file.DeleteAsync();
                }
            }
        }
        catch { }
    }

    private static async Task LoadImageDataAsync(BingWallpaperImage? image)
    {
        if (image == null) return;
        string cp = image.Copyright ?? string.Empty;
        string[] cps = cp.Split('('); // Copyright contains the caption of the image followed by the actual copyright text in parenthesis
        if (cps.Length == 2)
        {
            image.Caption = cps[0].Trim();
            image.Copyright = cps[1].Remove(cps[1].Length - 1, 1); // To remove the trailing parenthesis
        }
        StorageFile? imageFile = await GetImageFileAsync(image, PreferredResolution, PreferredOrientation);
        if (imageFile != null)
        {
            using IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read);
            BitmapImage bitmapImage = new();

            await bitmapImage.SetSourceAsync(fileStream);
            image.Thumbnail = bitmapImage;
            image.ImageFile = imageFile;
        }
    }

    public static async Task<bool> SetWallpaperAsync(BingWallpaperImage? image)
    {
#if ANDROID || WINDOWS
        if (image != null && image.ImageFile != null)
        {
            return await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(image.ImageFile);
        }
#endif
        return false;
    }

    private static async Task<StorageFile?> GetImageFileAsync(BingWallpaperImage? image, Resolution resolution = Resolution.High, Orientation orientation = Orientation.Landscape)
    {
        if (image != null)
        {
            try
            {
                string imgPath = image.UrlBase + "_" + GetResolutionSuffix(resolution, orientation) + ".jpg";
                string filename = GetFileName(imgPath);

                // Tries to get the already cached image
                StorageFile? imageFile = await localCacheFolder.TryGetItemAsync(filename) as StorageFile;
                if (imageFile == null && NetworkInterface.GetIsNetworkAvailable()) // If not present locally, then download and save them
                {
                    using HttpClient client = new();
                    HttpResponseMessage response = await client.GetAsync(BingUrl + imgPath);
                    if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        imageFile = await localCacheFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                        using IRandomAccessStream stream = await imageFile.OpenAsync(FileAccessMode.ReadWrite);
                        await response.Content.CopyToAsync(stream.AsStreamForWrite());
                        await stream.FlushAsync();
                    }
                }
                return imageFile;
            }
            catch { }
        }
        return null;
    }

    private static string GetFileName(string imgPath)
    {
        if (!string.IsNullOrEmpty(imgPath) && !string.IsNullOrWhiteSpace(imgPath))
        {
            return imgPath.Remove(0, 11); // Removes "/th?id=OHR."
        }

        return string.Empty;
    }

    private static string GetResolutionSuffix(Resolution resolution, Orientation orientation)
    {
        return resolution switch
        {
            Resolution.Low => orientation == Orientation.Landscape ? "1366x768" : "768x1366",
            Resolution.High => orientation == Orientation.Landscape ? "1920x1080" : "1080x1920",
            Resolution.UHD => "UHD", // Regardless of Orientation
            _ => string.Empty
        };
    }
}

public class BingWallpaperResponse
{
    [JsonPropertyName("images")]
    public BingWallpaperImage[]? Images { get; set; }
}

public class BingWallpaperImage
{
    //public string? startdate { get; set; }

    //public string? fullstartdate { get; set; }

    //public string? enddate { get; set; }

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

    public BitmapImage? Thumbnail { get; set; }

    [JsonIgnore]
    public StorageFile? ImageFile { get; set; }

    //public string? quiz { get; set; }

    //public bool wp { get; set; }

    //public string? hsh { get; set; }

    //public int drk { get; set; }

    //public int top { get; set; }

    //public int bot { get; set; }

    //public object[]? hs { get; set; }
}
