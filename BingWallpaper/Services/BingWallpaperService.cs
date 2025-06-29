using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Web;
using System.Xml.Linq;
using BingWallpaper.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Orientation = BingWallpaper.Models.Orientation;

namespace BingWallpaper.Services;

public class BingWallpaperService
{
    public const string BingUrl = "https://www.bing.com";
    public const string BingWallpaperUrl = "https://www.bing.com/HPImageArchive.aspx";
    private const int ImageCount = 8;
    private const string JsonExtension = ".json";

    public static ObservableCollection<BingWallpaperImage> Images { get; } = [];

    private static StorageFolder? _localCacheFolder;

    public static async Task FetchImagesAsync()
    {
        try
        {
            _localCacheFolder ??= ApplicationData.Current.LocalFolder;
            string fileName = $"{DateTime.Now:yyyy-MM-dd}{JsonExtension}";
            string json = string.Empty;
            StorageFile? jsonFile = null;

            if (await _localCacheFolder.TryGetItemAsync(fileName) is StorageFile file)
            {
                json = await FileIO.ReadTextAsync(file);
                jsonFile = file;
            }
            else if (NetworkInterface.GetIsNetworkAvailable())
            {
                var query = HttpUtility.ParseQueryString(string.Empty);
                query.Add("format", "js");
                query.Add("idx", "0");
                query.Add("n", ImageCount.ToString());
                string newUrl = $"{BingWallpaperUrl}?{query}";

                using HttpClient client = new();
                HttpResponseMessage response = await client.GetAsync(newUrl);
                if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    json = await response.Content.ReadAsStringAsync();
                    jsonFile = await _localCacheFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(jsonFile, json);
                }
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                var bingResponse = JsonSerializer.Deserialize<BingWallpaperResponse>(json);
                if (bingResponse?.Images?.Length > 0)
                {
                    Images.Clear();
                    foreach (var image in bingResponse.Images)
                    {
                        await LoadImageDataAsync(image);
                        Images.Add(image);
                    }
                }
            }

            await ClearUnusedCacheFilesAsync(jsonFile);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in FetchImagesAsync: {ex}");
            throw;
        }
    }

    private static async Task ClearUnusedCacheFilesAsync(StorageFile? jsonFile)
    {
        try
        {
            if (_localCacheFolder is null) return;

            var neededFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (jsonFile != null) neededFiles.Add(jsonFile.Path);
            foreach (var image in Images)
            {
                if (image.ImageFile != null)
                    neededFiles.Add(image.ImageFile.Path);
            }

            var files = await _localCacheFolder.GetFilesAsync();
            foreach (var file in files)
            {
                if (!neededFiles.Contains(file.Path))
                {
                    await file.DeleteAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ClearUnusedCacheFilesAsync: {ex}");
        }
    }

    private static async Task LoadImageDataAsync(BingWallpaperImage? image)
    {
        if (image is null) return;

        if (!string.IsNullOrWhiteSpace(image.Copyright))
        {
            var cps = image.Copyright.Split('(');
            if (cps.Length == 2)
            {
                image.Caption = cps[0].Trim();
                image.Copyright = cps[1].TrimEnd(')');
            }
        }

        var imageFile = await GetImageFileAsync(image, UserPreferences.Current.PreferredResolution, UserPreferences.Current.PreferredOrientation);
        if (imageFile != null)
        {
            using IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read);
            BitmapImage bitmapImage = new();
            await bitmapImage.SetSourceAsync(fileStream);
            image.Thumbnail = bitmapImage;
            image.ImageFile = imageFile;
        }
    }

    private static async Task<StorageFile?> GetImageFileAsync(BingWallpaperImage? image, Resolution resolution = Resolution.High, Orientation orientation = Orientation.Landscape)
    {
        if (image is null || _localCacheFolder is null) return null;

        try
        {
            string imgPath = $"{image.UrlBase}_{GetResolutionSuffix(resolution, orientation)}.jpg";
            string filename = GetFileName(imgPath);

            var imageFile = await _localCacheFolder.TryGetItemAsync(filename) as StorageFile;
            if (imageFile == null && NetworkInterface.GetIsNetworkAvailable())
            {
                using HttpClient client = new();
                HttpResponseMessage response = await client.GetAsync(BingUrl + imgPath);
                if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    imageFile = await _localCacheFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                    using IRandomAccessStream stream = await imageFile.OpenAsync(FileAccessMode.ReadWrite);
                    await response.Content.CopyToAsync(stream.AsStreamForWrite());
                    await stream.FlushAsync();
                }
            }
            return imageFile;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetImageFileAsync: {ex}");
            return null;
        }
    }

    private static string GetFileName(string imgPath)
    {
        if (string.IsNullOrWhiteSpace(imgPath)) return string.Empty;
        const string prefix = "/th?id=OHR.";
        return imgPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? imgPath[prefix.Length..]
            : imgPath;
    }

    private static string GetResolutionSuffix(Resolution resolution, Orientation orientation)
    {
        return resolution switch
        {
            Resolution.Low => orientation == Orientation.Landscape ? "1366x768" : "768x1366",
            Resolution.High => orientation == Orientation.Landscape ? "1920x1080" : "1080x1920",
            Resolution.UHD => "UHD",
            _ => string.Empty
        };
    }

    public static async Task<bool> SetWallpaperAsync(BingWallpaperImage? image, SetWallpaperPreference preference)
    {
#if ANDROID || WINDOWS
        if (image?.ImageFile != null)
        {
            return preference switch
            {
                SetWallpaperPreference.Homescreen => await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(image.ImageFile),
                SetWallpaperPreference.Lockscreen => await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(image.ImageFile),
                SetWallpaperPreference.Both => await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(image.ImageFile)
                    && await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(image.ImageFile),
                _ => false,
            };
        }
#endif
        return false;
    }

    public static async Task SaveWallpaperAsync(BingWallpaperImage? image)
    {
        if (image?.ImageFile is StorageFile imageFile)
        {
            var fileSavePicker = new FileSavePicker
            {
                SuggestedFileName = imageFile.Name
            };
            fileSavePicker.FileTypeChoices.Add("JPG File", [".jpg"]);

#if WINDOWS && !HAS_UNO
            if (App.Current is App { MainWindow: Window w })
            {
                nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(w);
                WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hwnd);
            }
#endif
            var saveFile = await fileSavePicker.PickSaveFileAsync();
            if (saveFile != null)
            {
                CachedFileManager.DeferUpdates(saveFile);
                var source = await imageFile.OpenStreamForReadAsync();
                var destination = await saveFile.OpenStreamForWriteAsync();
                await source.CopyToAsync(destination);
                await source.FlushAsync();
                await destination.FlushAsync();
                await CachedFileManager.CompleteUpdatesAsync(saveFile);
            }
        }
    }
}
