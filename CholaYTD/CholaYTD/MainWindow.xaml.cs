using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private readonly YoutubeClient YoutubeClient = new YoutubeClient ();
        private readonly Cli FfmpegCli = new Cli ( "ffmpeg.exe" );
        private readonly string TempDirectoryPath = Path.Combine ( Directory.GetCurrentDirectory (), "Temp" );
        private readonly string OutputDirectoryPath = Path.Combine ( Directory.GetCurrentDirectory (), "Output" );

        public MainWindow()
        {
            InitializeComponent ();
        }

        //private void btn_selCarpeta_Click( object sender, RoutedEventArgs e )
        //{
        //    WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog ();
        //    folderDialog.ShowNewFolderButton = false;
        //    folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
        //    WinForms.DialogResult result = folderDialog.ShowDialog ();

        //    if ( result == WinForms.DialogResult.OK )
        //    {
        //        string sPath = folderDialog.SelectedPath;
        //        tB_carpetaDestino.Text = sPath;
        //    }
        //}

        private void btn_desc_Click( object sender, RoutedEventArgs e )
        {
            string[] args = { "https://www.youtube.com/watch?v=fJ9rUzIMcZQ" };

            //YoutubeDownloader.MainAsync ( args );
            pB_barraProgreso.Value = 0;
            tB_barraProgresoText.Text = "";
            btn_descSWF.IsEnabled = false;
            DistribuidorTipoDescargas ( args );
        }

        private async Task descargarVideoHDAsync(string id)
        {
            // Get video info
            updateProgressBar ( 11, "Recogiendo información del video..." );
            var video = await YoutubeClient.GetVideoAsync ( id );
            updateProgressBar ( 13, "...información de video recogida." );
            updateProgressBar ( 13, "Renombrando..." );
            var cleanTitle = video.Title.Replace ( Path.GetInvalidFileNameChars (), '_' );
            updateProgressBar ( 15, "... video renombrado." );

            // Get best streams
            var streamInfoSet = await YoutubeClient.GetVideoMediaStreamInfosAsync ( id );
            updateProgressBar ( 17, "Buscando mejor stream..." );
            var videoStreamInfo = streamInfoSet.Video.WithHighestVideoQuality ();
            updateProgressBar ( 19, "Buscando la mayor calidad..." );
            var audioStreamInfo = streamInfoSet.Audio.WithHighestBitrate ();
            updateProgressBar ( 21, "Buscando el mejor sonido..." );

            // Download streams
            updateProgressBar ( 22, "Creando directorio temporal..." );
            Directory.CreateDirectory ( TempDirectoryPath );
            updateProgressBar ( 23, "Extrayendo extensión de archivos..." );
            var videoStreamFileExt = videoStreamInfo.Container.GetFileExtension ();
            updateProgressBar ( 24, "Creando rutas de archivos..." );
            var videoStreamFilePath = Path.Combine ( TempDirectoryPath, $"VID-{Guid.NewGuid ()}.{videoStreamFileExt}" );
            updateProgressBar ( 25, "Descargando el video..." );
            await YoutubeClient.DownloadMediaStreamAsync ( videoStreamInfo, videoStreamFilePath );
            updateProgressBar ( 36, "... video descargado." );
            var audioStreamFileExt = audioStreamInfo.Container.GetFileExtension ();
            var audioStreamFilePath = Path.Combine ( TempDirectoryPath, $"AUD-{Guid.NewGuid ()}.{audioStreamFileExt}" );
            updateProgressBar ( 39, "Descargando el sonido..." );
            await YoutubeClient.DownloadMediaStreamAsync ( audioStreamInfo, audioStreamFilePath );
            updateProgressBar ( 49, "...sonido descargado." );

            // Mux streams
            updateProgressBar ( 50, "Creando directorio de salida..." );
            Directory.CreateDirectory ( OutputDirectoryPath );
            var outputFilePath = Path.Combine ( OutputDirectoryPath, $"{cleanTitle}.mp4" );
            updateProgressBar ( 60, "Combinando video y audio..." );
            await FfmpegCli.ExecuteAsync ( $"-i \"{videoStreamFilePath}\" -i \"{audioStreamFilePath}\" -shortest \"{outputFilePath}\" -y" );
            updateProgressBar ( 98, "... video y audio combinados con éxito." );

            // Delete temp files
            updateProgressBar ( 99, "Eliminando archivos temporales..." );
            File.Delete ( videoStreamFilePath );
            File.Delete ( audioStreamFilePath );
            updateProgressBar ( 100, "... proceso finalizado." );
        }

        private async Task descargarListaReproduccionHDAsync( string id )
        {
            // Get playlist info
            updateProgressBar ( 7, "Recogiendo informacion de lista de reproducción..." );
            var playlist = await YoutubeClient.GetPlaylistAsync ( id );
            updateProgressBar (9, "Información de lista de reproducción recogida.");

            // Work on the videos
            int numVideo = 1;
            foreach ( var video in playlist.Videos )
            {
                updateProgressBar ( 10, "Empezando con el video Nº: " + numVideo );
                numVideo++;
                await descargarVideoHDAsync ( video.Id );
            }
        }

        private async Task DistribuidorTipoDescargas( string[] args )
        {
            foreach ( var arg in args )
            {
                // Try to determine the type of the URL/ID that was given
                updateProgressBar ( 1, "Determinando tipo de enlace..." );
                // Playlist ID
                if ( YoutubeClient.ValidatePlaylistId ( arg ) )
                {
                    updateProgressBar (5, "Determinando tipo de enlace...");
                    await descargarListaReproduccionHDAsync ( arg );
                }
                
                // Playlist URL
                else if ( YoutubeClient.TryParsePlaylistId ( arg, out string playlistId ) )
                {
                    updateProgressBar ( 5, "Determinando tipo de enlace..." );
                    await descargarListaReproduccionHDAsync ( playlistId );
                }

                // Video ID
                else if ( YoutubeClient.ValidateVideoId ( arg ) )
                {
                    updateProgressBar ( 5, "Determinando tipo de enlace..." );
                    await descargarVideoHDAsync ( arg );
                }

                // Video URL
                else if ( YoutubeClient.TryParseVideoId ( arg, out string videoId ) )
                {
                    updateProgressBar ( 5, "Determinando tipo de enlace..." );
                    await descargarVideoHDAsync ( videoId );
                }

                // Unknown
                else
                {
                    updateProgressBar ( 0, "Error: el enlace no parece válido." );
                    throw new ArgumentException ( $"Unrecognized URL or ID: [{arg}]", nameof ( arg ) );
                }
            }
            updateProgressBar (0, "Todas las tareas finalizadas." );
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\Output");
        }
    
        private void updateProgressBar (int n, string msg )
        {
            Action action = () => { setProgress ( n, msg ); };
            pB_barraProgreso.Dispatcher.BeginInvoke ( action );
        }

        private void setProgress(int n, string msg)
        {
            pB_barraProgreso.Value = n;
            tB_barraProgresoText.Text = msg;
            if ( msg.Equals ( "Todas las tareas finalizadas." ) )
                btn_descSWF.IsEnabled = true;
            
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
