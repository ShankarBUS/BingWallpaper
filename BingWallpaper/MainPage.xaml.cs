using Windows.System;
using BingWallpaper.Models;
using BingWallpaper.Services;

namespace BingWallpaper;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        resCmb.ItemsSource = Enum.GetValues(typeof(Resolution));
        orCmb.ItemsSource = Enum.GetValues(typeof(Models.Orientation));
        swCmb.ItemsSource = Enum.GetValues(typeof(SetWallpaperPreference));
        Loaded += MainPage_Loaded;
        swCB.Checked += (s, e) => swCmb.Visibility = Visibility.Collapsed;
        swCB.Unchecked += (s, e) => swCmb.Visibility = Visibility.Visible;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await GetWallpapersAsync();
    }

    private async void GetWallpapersButton_Click(object sender, RoutedEventArgs e)
    {
        await GetWallpapersAsync();
    }

    private async Task GetWallpapersAsync()
    {
        progressRing.IsActive = true;
        await BingWallpaperService.FetchImagesAsync();
        flipView.ItemsSource = BingWallpaperService.Images;
        progressRing.IsActive = false;
    }

    private async void SetWallpaperButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserPreferences.Current.AlwaysAskSetPreference)
        {
            setWPFlyout.ShowAt(setWPBtn);
        }
        else
            await TrySetWallpaperAsync(UserPreferences.Current.SetWallpaperPreference);
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        SetWallpaperPreference preference = (sender as Button)?.Tag switch
        {
            "#home" => SetWallpaperPreference.Homescreen,
            "#lock" => SetWallpaperPreference.Lockscreen,
            "#both" => SetWallpaperPreference.Both,
            _ => SetWallpaperPreference.Homescreen,
        };
        if (remCB.IsChecked == true)
        {
            UserPreferences.Current.AlwaysAskSetPreference = false;
            UserPreferences.Current.SetWallpaperPreference = preference;
            remCB.IsChecked = false;
        }
        setWPFlyout.Hide();

        await TrySetWallpaperAsync(preference);
    }

    private async Task TrySetWallpaperAsync(SetWallpaperPreference preference = SetWallpaperPreference.Homescreen)
    {
        infoBar.IsOpen = false;
        bool result = await BingWallpaperService.SetWallpaperAsync(flipView.SelectedItem as BingWallpaperImage, preference);
        if (result)
        {
            infoBar.IsOpen = true;
            await Task.Run(() => Thread.Sleep(2000));
            infoBar.IsOpen = false;
        }
    }

    private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton hyperlinkButton && hyperlinkButton.DataContext is BingWallpaperImage image)
        {
            try
            {
                await Launcher.LaunchUriAsync(new(image.CopyrightLink ?? string.Empty));
            }
            catch { }
        }
    }

    private async void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        try
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/ShankarBUS/BingWallpaper"));
        }
        catch { }
    }

    private async void SaveWallpaperButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await BingWallpaperService.SaveWallpaperAsync(flipView.SelectedItem as BingWallpaperImage);
        }
        catch { }
    }
}
