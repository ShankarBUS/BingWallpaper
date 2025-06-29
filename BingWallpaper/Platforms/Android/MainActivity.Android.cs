using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.Work;

namespace BingWallpaper.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : ApplicationActivity
{
    public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
    {
        base.OnCreate(savedInstanceState, persistentState);

        var workRequest = PeriodicWorkRequest.Builder.From<WallpaperUpdateWorker>(TimeSpan.FromDays(1))
            .Build();

        WorkManager.GetInstance(ApplicationContext).EnqueueUniquePeriodicWork(
            "BingWallpaperUpdateWork",
            ExistingPeriodicWorkPolicy.Keep,
            workRequest
        );
    }
}
