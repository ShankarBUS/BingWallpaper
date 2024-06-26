using BingWallpaper.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BingWallpaper;

public class UserPreferences : ObservableObject
{
    public UserPreferences()
    {
#if ANDROID
        preferredResolution = Resolution.High;
        preferredOrientation = Services.Orientation.Portrait;
#else
        preferredResolution = Resolution.UHD;
        preferredOrientation = Services.Orientation.Landscape;
#endif
        alwaysAskSetPreference = true;
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(PreferredResolution), out var value) &&
            Enum.TryParse(typeof(Resolution), value.ToString(), out var result) && result is Resolution resolution)
        {
            preferredResolution = resolution;
        }
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(PreferredOrientation), out var value1) &&
            Enum.TryParse(typeof(Services.Orientation), value1.ToString(), out var result1) && result1 is Services.Orientation orientation)
        {
            preferredOrientation = orientation;
        }
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(SetWallpaperPreference), out var value2) &&
            Enum.TryParse(typeof(SetWallpaperPreference), value2.ToString(), out var result2) && result2 is SetWallpaperPreference _setWallpaperPreference)
        {
            setWallpaperPreference = _setWallpaperPreference;
        }
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(nameof(AlwaysAskSetPreference), out var value3) &&
            bool.TryParse(value3.ToString(), out bool result3))
        {
            alwaysAskSetPreference = result3;
        }
    }

    public static UserPreferences Current { get; } = new();

    private Resolution preferredResolution;

    public Resolution PreferredResolution
    {
        get => preferredResolution;
        set
        {
            if (SetProperty(ref preferredResolution, value))
                ApplicationData.Current.LocalSettings.Values[nameof(PreferredResolution)] = value.ToString();
        }
    }


    private Services.Orientation preferredOrientation;

    public Services.Orientation PreferredOrientation
    {
        get => preferredOrientation;
        set
        {
            if (SetProperty(ref preferredOrientation, value))
                ApplicationData.Current.LocalSettings.Values[nameof(PreferredOrientation)] = value.ToString();
        }
    }

    private SetWallpaperPreference setWallpaperPreference;

    public SetWallpaperPreference SetWallpaperPreference
    {
        get => setWallpaperPreference;
        set
        {
            if (SetProperty(ref setWallpaperPreference, value))
                ApplicationData.Current.LocalSettings.Values[nameof(SetWallpaperPreference)] = value.ToString();
        }
    }

    private bool alwaysAskSetPreference;

    public bool AlwaysAskSetPreference
    {
        get => alwaysAskSetPreference;
        set
        {
            if (SetProperty(ref alwaysAskSetPreference, value))
                ApplicationData.Current.LocalSettings.Values[nameof(AlwaysAskSetPreference)] = value.ToString();
        }
    }
}
