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
    public Bitmap Cover { get; set; }
    public WebClient Client { get; set; }

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        Client = new WebClient();
    }

    public async void ParsePlaylist()
    {
        string url = this.FindControl<TextBox>("UrlInput").Text;

        if (string.IsNullOrEmpty(url) || !url.Contains("music.amazon"))
        {
            var messageBox = new Window
            {
                Content = new TextBlock { Text = "Enter the valid amazon music url" },
                Title = "Alert",
                SizeToContent = SizeToContent.WidthAndHeight,
            };

            messageBox.ShowDialog(this);

            return;
        }

        using (var driver = new ChromeDriver())
        {
            StringBuilder description = new StringBuilder();
            Songs = new List<Song>();
            string playlistTitle = string.Empty;
            string image = string.Empty;
            string basePath = string.Empty;

            if (url.Contains("albums", StringComparison.InvariantCultureIgnoreCase))
            {
                basePath = "//music-text-row[contains(@icon-name,'play')]";
            }
            else
            {
                basePath = "//music-image-row[contains(@icon-name,'play')]";
            }

            driver.Manage().Window.Maximize();
           
            try
            {
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(3000);

                image = driver.FindElement(By.XPath("//music-detail-header[contains(@image-kind,'square')]"))
               .GetDomAttribute("image-src");

                playlistTitle = driver.FindElement(By.XPath("//music-detail-header[contains(@image-kind,'square')]"))
                    .GetDomAttribute("headline");

                description.Append(driver.FindElement(By.XPath("//music-detail-header[contains(@image-kind,'square')]"))
                    .GetDomAttribute("primary-text"))
                    .Append(" ")
                    .Append(driver.FindElement(By.XPath("//music-detail-header[contains(@image-kind,'square')]"))
                    .GetDomAttribute("secondary-text"))
                    .Append(" ")
                    .Append(driver.FindElement(By.XPath("//music-detail-header[contains(@image-kind,'square')]"))
                    .GetDomAttribute("tertiary-text"));

                var durations = driver.FindElements(
                     By.XPath(basePath + "//div[@class='col4']//span"));
                await DownloadImage(image);

                var songs = driver.FindElements(By.XPath(basePath));

                for (int i = 0; i < songs.Count; i++)
                {
                    Songs.Add(new Song
                    {
                        SongName = songs[i].GetDomAttribute("primary-text"),
                        AlbumName = songs[i].GetDomAttribute("secondary-text-2"),
                        ArtistName = songs[i].GetDomAttribute("secondary-text-1"),
                        Duration = durations[i].Text
                    });
                };
            }
            catch (Exception ex)
            {
                var messageBox = new Window
                {
                    Content = new TextBlock { Text = ex.Message },
                    Title = "Alert",
                    SizeToContent = SizeToContent.WidthAndHeight,
                };

                messageBox.ShowDialog(this);

            }

            this.FindControl<ListBox>("Playlist").Items = Songs;
            this.FindControl<TextBlock>("PlaylistTitle").Text = playlistTitle;
            this.FindControl<TextBlock>("PlaylistDecsription").Text = description.ToString();
        }
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        ParsePlaylist();
    }

    public async Task DownloadImage(string url)
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

    public void DownloadComplete(byte[] bytes)
    {
        try
        {
            Stream stream = new MemoryStream(bytes);
            var image = new Bitmap(stream);

            Cover = image;

            if (this.FindControl<Image>("CoverImage") != null)
            {
                this.FindControl<Image>("CoverImage").Source = Cover;
            }

        }
        catch
        {
            Cover = null;
        }
    }
}
