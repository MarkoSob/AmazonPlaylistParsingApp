using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Net;
using System.IO;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
using AmazonPlaylistParsingApp.Entities;

namespace AmazonPlaylistParsingApp;

public partial class MainWindow : Window
{
    public List<Song> Songs { get; set; }
    public Bitmap CoverImage { get; set; }
    public WebClient Client { get; set; }
    public StringBuilder PlaylistDescriprtion { get; set; }
    public ChromeDriver Driver { get; set; }
    public string PlaylistUrl { get; set; }
    public string ParsedPlaylistTitle { get; set; }
    public string CoverImageUrl { get; set; }
    public string BaseSongElementPath { get; set; }

    private const string elementsPath = "//music-detail-header[contains(@image-kind,'square')]";

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        Client = new WebClient();
    }

    private async void ParsePlaylist()
    {
        if (!GetUserInput())
        {
            return;
        }

        using (Driver = new ChromeDriver())
        {
            Songs = new List<Song>();

            if (PlaylistUrl.Contains("albums", StringComparison.InvariantCultureIgnoreCase))
            {
                BaseSongElementPath = "//music-text-row[contains(@icon-name,'play')]";
            }
            else
            {
                BaseSongElementPath = "//music-image-row[contains(@icon-name,'play')]";
            }

            Driver.Navigate().GoToUrl(PlaylistUrl);

            Thread.Sleep(3000);

            try
            {
                GetPlayllistElements();
            }
            catch (NoSuchElementException ex)
            {
                ShowErrorMessage(ex.Message);
            }

            if (this.FindControl<ListBox>("Playlist") != null
                && this.FindControl<TextBlock>("PlaylistTitle") != null
                && this.FindControl<TextBlock>("PlaylistDecsription") != null)
            {
                this.FindControl<ListBox>("Playlist").Items = Songs;
                this.FindControl<TextBlock>("PlaylistTitle").Text = ParsedPlaylistTitle;
                this.FindControl<TextBlock>("PlaylistDecsription").Text = PlaylistDescriprtion.ToString();
            }
        }
    }

    private bool GetUserInput()
    {
        PlaylistUrl = string.Empty;

        if (this.FindControl<TextBox>("UrlInput") != null)
        {
            PlaylistUrl = this.FindControl<TextBox>("UrlInput").Text;
        }

        if (string.IsNullOrEmpty(PlaylistUrl) || !PlaylistUrl.Contains("music.amazon"))
        {
            ShowErrorMessage("Enter the valid amazon music url");

            return false;
        }

        return true;
      
    }

    private async void GetPlayllistElements()
    {
        PlaylistDescriprtion = new StringBuilder();

        CoverImageUrl = Driver.FindElement(By.XPath(elementsPath))
            .GetDomAttribute("image-src");

        ParsedPlaylistTitle = Driver.FindElement(By.XPath(elementsPath))
            .GetDomAttribute("headline");

        PlaylistDescriprtion
            .Append(Driver.FindElement(By.XPath(elementsPath))
            .GetDomAttribute("primary-text"))
            .Append(" ")
            .Append(Driver.FindElement(By.XPath(elementsPath))
            .GetDomAttribute("secondary-text"))
            .Append(" ")
            .Append(Driver.FindElement(By.XPath(elementsPath))
            .GetDomAttribute("tertiary-text"));

        var songsDuration = Driver.FindElements(
             By.XPath(BaseSongElementPath + "//div[@class='col4']//span"));

        var songElements = Driver.FindElements(By.XPath(BaseSongElementPath));

        for (int i = 0; i < songElements.Count; i++)
        {
            Songs.Add(new Song
            {
                SongName = songElements[i].GetDomAttribute("primary-text"),
                AlbumName = songElements[i].GetDomAttribute("secondary-text-2"),
                ArtistName = songElements[i].GetDomAttribute("secondary-text-1"),
                Duration = songsDuration[i].Text
            });
        };

        await DownloadImage(CoverImageUrl);
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        ParsePlaylist();
    }

    private async Task DownloadImage(string url)
    {
        try
        {
            var task = Client.DownloadDataTaskAsync(new Uri(url));
            byte[] bytes = await task;

            if (task.IsCompletedSuccessfully)
            {
                DownloadComplete(bytes);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void DownloadComplete(byte[] bytes)
    {
        try
        {
            Stream stream = new MemoryStream(bytes);
            var image = new Bitmap(stream);

            CoverImage = image;

            if (this.FindControl<Image>("PlaylistImage") != null)
            {
                this.FindControl<Image>("PlaylistImage").Source = CoverImage;
            }

        }
        catch
        {
            CoverImage = null;
        }
    }

    private void ShowErrorMessage(string errorMessage)
    {
        var messageBox = new Window
        {
            Content = new TextBlock { Text = errorMessage },
            Title = "Error",
            SizeToContent = SizeToContent.WidthAndHeight,
        };

        messageBox.ShowDialog(this);
    }
}
