using Android.App;
using Android.Runtime;

[assembly: UsesPermission("android.permission.SET_WALLPAPER")]
namespace BingWallpaper.Droid;

[Application(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/android_icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/AppTheme"
)]
public class Application : NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }
}

