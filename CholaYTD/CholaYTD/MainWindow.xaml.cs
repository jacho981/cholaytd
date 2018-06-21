using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CliWrap;
using Tyrrrz.Extensions;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;
using WinForms = System.Windows.Forms;

namespace CholaYTD
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly YoutubeClient YoutubeClient = new YoutubeClient ();
        private static readonly Cli FfmpegCli = new Cli ( "ffmpeg.exe" );
        private static readonly string TempDirectoryPath = Path.Combine ( Directory.GetCurrentDirectory (), "Temp" );
        private static readonly string OutputDirectoryPath = Path.Combine ( Directory.GetCurrentDirectory (), "Output" );

        public MainWindow()
        {
            InitializeComponent ();
        }

 



        private void btn_selCarpeta_Click( object sender, RoutedEventArgs e )
        {
            WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog ();
            folderDialog.ShowNewFolderButton = false;
            folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
            WinForms.DialogResult result = folderDialog.ShowDialog ();

            if ( result == WinForms.DialogResult.OK )
            {
                string sPath = folderDialog.SelectedPath;
                tB_carpetaDestino.Text = sPath;
            }
        }

        private void btn_desc_Click( object sender, RoutedEventArgs e )
        {
            string[] args = { "https://www.youtube.com/watch?v=fJ9rUzIMcZQ" };
            DistribuidorTipoDescargas ( args ).GetAwaiter ().GetResult ();
        }

        private static async Task descargarVideoHDAsync(string id)
        {
            Console.WriteLine ( $"Working on video [{id}]..." );

            // Get video info
            var video = await YoutubeClient.GetVideoAsync ( id );
            var cleanTitle = video.Title.Replace ( Path.GetInvalidFileNameChars (), '_' );
            Console.WriteLine ( $"{video.Title}" );

            // Get best streams
            var streamInfoSet = await YoutubeClient.GetVideoMediaStreamInfosAsync ( id );
            var videoStreamInfo = streamInfoSet.Video.WithHighestVideoQuality ();
            var audioStreamInfo = streamInfoSet.Audio.WithHighestBitrate ();

            // Download streams
            Console.WriteLine ( "Downloading..." );
            Directory.CreateDirectory ( TempDirectoryPath );
            var videoStreamFileExt = videoStreamInfo.Container.GetFileExtension ();
            var videoStreamFilePath = Path.Combine ( TempDirectoryPath, $"VID-{Guid.NewGuid ()}.{videoStreamFileExt}" );
            await YoutubeClient.DownloadMediaStreamAsync ( videoStreamInfo, videoStreamFilePath );
            var audioStreamFileExt = audioStreamInfo.Container.GetFileExtension ();
            var audioStreamFilePath = Path.Combine ( TempDirectoryPath, $"AUD-{Guid.NewGuid ()}.{audioStreamFileExt}" );
            await YoutubeClient.DownloadMediaStreamAsync ( audioStreamInfo, audioStreamFilePath );

            // Mux streams
            Console.WriteLine ( "Combining..." );
            Directory.CreateDirectory ( OutputDirectoryPath );
            var outputFilePath = Path.Combine ( OutputDirectoryPath, $"{cleanTitle}.mp4" );
            await FfmpegCli.ExecuteAsync ( $"-i \"{videoStreamFilePath}\" -i \"{audioStreamFilePath}\" -shortest \"{outputFilePath}\" -y" );

            // Delete temp files
            Console.WriteLine ( "Deleting temp files..." );
            File.Delete ( videoStreamFilePath );
            File.Delete ( audioStreamFilePath );

            Console.WriteLine ( $"Downloaded video [{id}] to [{outputFilePath}]" );

        }

        private static async Task descargarListaReproduccionHDAsync( string id )
        {
            Console.WriteLine ( $"Working on playlist [{id}]..." );

            // Get playlist info
            var playlist = await YoutubeClient.GetPlaylistAsync ( id );
            Console.WriteLine ( $"{playlist.Title} ({playlist.Videos.Count} videos)" );

            // Work on the videos
            Console.WriteLine ();
            foreach ( var video in playlist.Videos )
            {
                await descargarVideoHDAsync ( video.Id );
                Console.WriteLine ();
            }
        }

        private static async Task DistribuidorTipoDescargas( string[] args )
        {
            foreach ( var arg in args )
            {
                // Try to determine the type of the URL/ID that was given

                // Playlist ID
                if ( YoutubeClient.ValidatePlaylistId ( arg ) )
                {
                    await descargarListaReproduccionHDAsync ( arg );
                }

                // Playlist URL
                else if ( YoutubeClient.TryParsePlaylistId ( arg, out string playlistId ) )
                {
                    await descargarListaReproduccionHDAsync ( playlistId );
                }

                // Video ID
                else if ( YoutubeClient.ValidateVideoId ( arg ) )
                {
                    await descargarVideoHDAsync ( arg );
                }

                // Video URL
                else if ( YoutubeClient.TryParseVideoId ( arg, out string videoId ) )
                {
                    await descargarVideoHDAsync ( videoId );
                }

                // Unknown
                else
                {
                    throw new ArgumentException ( $"Unrecognized URL or ID: [{arg}]", nameof ( arg ) );
                }

                Console.WriteLine ();
            }

            Console.WriteLine ( "Done" );
        }

        private async void descargarSoloVideo ()
        {
            string link = "https://www.youtube.com/watch?v=fJ9rUzIMcZQ";
            var url = link;
            var id = YoutubeClient.ParseVideoId ( url );

            var client = new YoutubeClient ();
            var streamInfoSet = await client.GetVideoMediaStreamInfosAsync ( id );
            var streamInfo = streamInfoSet.Video.WithHighestVideoQuality ();
            var ext = streamInfo.Container.GetFileExtension ();
            await client.DownloadMediaStreamAsync ( streamInfo, $"downloaded_video.{ext}" );
        }

        private async void descargarSoloAudio()
        {
            string link = "https://www.youtube.com/watch?v=fJ9rUzIMcZQ";
            var url = link;
            var id = YoutubeClient.ParseVideoId ( url );

            var client = new YoutubeClient ();
            var streamInfoSet = await client.GetVideoMediaStreamInfosAsync ( id );
            var streamInfo = streamInfoSet.Video.WithHighestVideoQuality ();
            var ext = streamInfo.Container.GetFileExtension ();
            await client.DownloadMediaStreamAsync ( streamInfo, $"downloaded_video.{ext}" );
        }
    }
}
