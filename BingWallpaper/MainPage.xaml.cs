using System.Collections.ObjectModel;
using Windows.System;
using BingWallpaper.Services;

namespace BingWallpaper;

public sealed partial class MainPage : Page
{
    private ObservableCollection<BingWallpaperImage>? images;

    public MainPage()
    {
        InitializeComponent();
        resCmb.ItemsSource = Enum.GetValues(typeof(Resolution));
        orCmb.ItemsSource = Enum.GetValues(typeof(Services.Orientation));
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        progressRing.IsActive = true;
        images = await BingWallpaperService.FetchImagesAsync();
        flipView.ItemsSource = images;
        progressRing.IsActive = false;
    }

    private async void GetWallpapersButton_Click(object sender, SplitButtonClickEventArgs e)
    {
        progressRing.IsActive = true;
        if (resCmb.SelectedItem is Resolution resolution)
            BingWallpaperService.PreferredResolution = resolution;
        if (orCmb.SelectedItem is Services.Orientation orientation)
            BingWallpaperService.PreferredOrientation = orientation;
        images = await BingWallpaperService.FetchImagesAsync();
        flipView.ItemsSource = images;
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
}
