using Android.Content;
using AndroidX.Work;
using BingWallpaper.Services;

namespace BingWallpaper.Droid;

public class WallpaperUpdateWorker(Context context, WorkerParameters workerParams) : Worker(context, workerParams)
{
    public override Result DoWork()
    {
        try
        {
            Task.Run(async () =>
            {
                await BingWallpaperService.FetchImagesAsync();
                await BingWallpaperService.SetWallpaperAsync(BingWallpaperService.Images.FirstOrDefault(), UserPreferences.Current.SetWallpaperPreference);
            }).GetAwaiter().GetResult();

            return Result.InvokeSuccess();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WallpaperUpdateWorker error: {ex}");
            return Result.InvokeFailure();
        }
    }
}
