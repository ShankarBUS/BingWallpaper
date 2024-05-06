using System.Collections.ObjectModel;
using Windows.System;
using BingWallpaper.Services;

namespace BingWallpaper;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        resCmb.ItemsSource = Enum.GetValues(typeof(Resolution));
        orCmb.ItemsSource = Enum.GetValues(typeof(Services.Orientation));
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await GetWallpapersAsync();
    }

    private async void GetWallpapersButton_Click(object sender, RoutedEventArgs e)
    {
        if (resCmb.SelectedItem is Resolution resolution)
            BingWallpaperService.PreferredResolution = resolution;
        if (orCmb.SelectedItem is Services.Orientation orientation)
            BingWallpaperService.PreferredOrientation = orientation;
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
        infoBar.IsOpen = false;
        bool result = await BingWallpaperService.SetWallpaperAsync(flipView.SelectedItem as BingWallpaperImage);
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
}
