using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CliWrap;
using Tyrrrz.Extensions;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;

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
        private int numVideoProgress = 0;

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
            //string[] args = { "https://www.youtube.com/watch?v=fJ9rUzIMcZQ" };

            pB_barraProgreso.Value = 0;
            tB_barraProgresoText.Text = "";

            btn_añadirEnlace.IsEnabled = false;
            btn_borrarEnlace.IsEnabled = false;
            rb_audio.IsEnabled = false;
            rb_normal.IsEnabled = false;
            rb_video.IsEnabled = false;
            btn_descSWF.IsEnabled = false;
            listaEnlaces.IsEnabled = false;
            tB_introEnlace.IsEnabled = false;

            btn_descSWF.Visibility = Visibility.Collapsed;
            grd_BarrasProgreso.Visibility = Visibility.Visible;

            string[] args;
            List<string> tempList = new List<string>();

            // cogemos string[] y de los elementos de la lista (EXTRAYENDO LAS URLs, NO los titulos)
            foreach (ListBoxItem item in listaEnlaces.Items) {
                tempList.Add((string)item.ToolTip);
            }            
            args = tempList.ToArray();

            // chekeamos el tipo de descarga elegida en el radioButton (video normal, solo video, solo audio)

            DistribuidorTipoDescargas ( args );
        }

        private async Task descargarVideoHDAsync(string id)
        {
            numVideoProgress++;
            // Get video info
            updateProgressBar ( 11, "Video #" + numVideoProgress + " - Recogiendo información del video..." );
            var video = await YoutubeClient.GetVideoAsync ( id );
            updateProgressBar ( 13, "Video #" + numVideoProgress + " - ...información de video recogida.");
            updateProgressBar ( 13, "Video #" + numVideoProgress + " - Renombrando...");
            var cleanTitle = video.Title.Replace ( Path.GetInvalidFileNameChars (), '_' );
            updateProgressBar ( 15, "Video #" + numVideoProgress + " - ... video renombrado.");

            // Get best streams
            var streamInfoSet = await YoutubeClient.GetVideoMediaStreamInfosAsync ( id );
            updateProgressBar ( 17, "Video #" + numVideoProgress + " - Buscando mejor stream...");
            var videoStreamInfo = streamInfoSet.Video.WithHighestVideoQuality ();
            updateProgressBar ( 19, "Video #" + numVideoProgress + " - Buscando la mayor calidad...");
            var audioStreamInfo = streamInfoSet.Audio.WithHighestBitrate ();
            updateProgressBar ( 21, "Video #" + numVideoProgress + " - Buscando el mejor sonido...");

            // Download streams
            updateProgressBar ( 22, "Video #" + numVideoProgress + " - Creando directorio temporal...");
            Directory.CreateDirectory ( TempDirectoryPath );
            updateProgressBar ( 23, "Video #" + numVideoProgress + " - Extrayendo extensión de archivos...");
            var videoStreamFileExt = videoStreamInfo.Container.GetFileExtension ();
            updateProgressBar ( 24, "Video #" + numVideoProgress + " - Creando rutas de archivos...");
            var videoStreamFilePath = Path.Combine ( TempDirectoryPath, $"VID-{Guid.NewGuid ()}.{videoStreamFileExt}" );
            updateProgressBar ( 25, "Video #" + numVideoProgress + " - Descargando el video...");
            await YoutubeClient.DownloadMediaStreamAsync ( videoStreamInfo, videoStreamFilePath );
            updateProgressBar ( 36, "Video #" + numVideoProgress + " - ... video descargado.");
            var audioStreamFileExt = audioStreamInfo.Container.GetFileExtension ();
            var audioStreamFilePath = Path.Combine ( TempDirectoryPath, $"AUD-{Guid.NewGuid ()}.{audioStreamFileExt}" );
            updateProgressBar ( 39, "Video #" + numVideoProgress + " - Descargando el sonido...");
            await YoutubeClient.DownloadMediaStreamAsync ( audioStreamInfo, audioStreamFilePath );
            updateProgressBar ( 49, "Video #" + numVideoProgress + " - ...sonido descargado.");

            // Mux streams
            updateProgressBar ( 50, "Video #" + numVideoProgress + " - Creando directorio de salida...");
            Directory.CreateDirectory ( OutputDirectoryPath );
            var outputFilePath = Path.Combine ( OutputDirectoryPath, $"{cleanTitle}.mp4" );
            updateProgressBar ( 60, "Video #" + numVideoProgress + " - Combinando video y audio...");
            await FfmpegCli.ExecuteAsync ( $"-i \"{videoStreamFilePath}\" -i \"{audioStreamFilePath}\" -shortest \"{outputFilePath}\" -y" );
            updateProgressBar ( 98, "Video #" + numVideoProgress + " - ... video y audio combinados con éxito.");

            // Delete temp files
            updateProgressBar ( 99, "Video #" + numVideoProgress + " - Eliminando archivos temporales...");
            File.Delete ( videoStreamFilePath );
            File.Delete ( audioStreamFilePath );
            updateProgressBar ( 100, "Video #" + numVideoProgress + " - ... proceso finalizado.");
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
            if ( msg.Equals ( "Todas las tareas finalizadas." ))
            {
                btn_descSWF.IsEnabled = true;
                btn_añadirEnlace.IsEnabled = true;
                btn_borrarEnlace.IsEnabled = true;
                rb_audio.IsEnabled = true;
                rb_normal.IsEnabled = true;
                rb_video.IsEnabled = true;
                listaEnlaces.IsEnabled = true;
                tB_introEnlace.IsEnabled = true;
                grd_BarrasProgreso.Visibility = Visibility.Collapsed;
                btn_descSWF.Visibility = Visibility.Visible;
                numVideoProgress = 0;
                popup_DownloadSuccess.IsOpen = true;                
            }
                
            
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

        private void tB_introEnlace_GotFocus(object sender, RoutedEventArgs e)
        {
            tB_introEnlace.Text = "";
        }

        private void btn_añadirEnlace_Click(object sender, RoutedEventArgs e)
        {
            if (YoutubeClient.ValidateVideoId(tB_introEnlace.Text.Substring(tB_introEnlace.Text.IndexOf('=')+1)) || YoutubeClient.ValidatePlaylistId(tB_introEnlace.Text.Substring(tB_introEnlace.Text.IndexOf('=') + 1)))
            {
                if (listaEnlaces.HasItems)
                {
                    bool addItemToList = true;
                    foreach (ListBoxItem lbi in listaEnlaces.Items)
                    {
                        if (lbi.ToolTip.Equals(tB_introEnlace.Text))
                        {
                            addItemToList = false;
                        }
                    }
                    if (addItemToList)
                    {
                        //ListBoxItem item = new ListBoxItem();
                        //item.Content = tB_introEnlace.Text;

                        RecogerInfoVideoLista(tB_introEnlace.Text);

                        //listaEnlaces.Items.Add(item);
                        //tB_introEnlace.Text = "Introduzca un enlace de un video o una lista de reproducción de Youtube...";
                    }
                    else
                    {
                        popup_errorLinkInList.IsOpen = true;
                        tB_introEnlace.Text = "Introduzca un enlace de un video o una lista de reproducción de Youtube...";
                    }
                }
                else
                {
                    //ListBoxItem item = new ListBoxItem();
                    //item.Content = tB_introEnlace.Text;

                    RecogerInfoVideoLista(tB_introEnlace.Text);

                    //listaEnlaces.Items.Add(item);
                    //tB_introEnlace.Text = "Introduzca un enlace de un video o una lista de reproducción de Youtube...";
                }
            }
            else
            {
                popup_errorLinkFormat.IsOpen = true;
                tB_introEnlace.Text = "Introduzca un enlace de un video o una lista de reproducción de Youtube...";
            }
        }

        private async Task RecogerInfoVideoLista(string tb_url)
        {
            Console.WriteLine("entra en tarea asyncrona");
            Console.WriteLine(YoutubeClient.ValidateVideoId(tb_url.Substring(tb_url.IndexOf('=') + 1)));
            if (YoutubeClient.ValidateVideoId(tb_url.Substring(tb_url.IndexOf('=') + 1)))
            {
                var video = await YoutubeClient.GetVideoAsync(tb_url.Substring(tb_url.IndexOf('=') + 1));
                string titulo = video.Title.Replace(Path.GetInvalidFileNameChars(), '_');
                setTitulosLista(titulo);
            }
            else if (YoutubeClient.ValidatePlaylistId(tb_url.Substring(tb_url.IndexOf('=') + 1)))
            {
                var playlist = await YoutubeClient.GetPlaylistAsync(tb_url.Substring(tb_url.IndexOf('=') + 1));
                string titulo = "(" + playlist.Videos.Count + " videos) " + playlist.Title;
                setTitulosLista(titulo);
            }
        }

        private void updateTitulosLista(string title)
        {
            Action action = () => { setTitulosLista(title); };
            listaEnlaces.Dispatcher.BeginInvoke(action);
        }

        private void setTitulosLista(string title)
        {
            ListBoxItem item = new ListBoxItem();
            item.Content = title;
            item.ToolTip = tB_introEnlace.Text;
            listaEnlaces.Items.Add(item);
            tB_introEnlace.Text = "Introduzca un enlace de un video o una lista de reproducción de Youtube...";
        }

        private void btn_borrarEnlace_Click(object sender, RoutedEventArgs e)
        {
            listaEnlaces.Items.RemoveAt(listaEnlaces.SelectedIndex);
        }

        private void grd_exit_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}
