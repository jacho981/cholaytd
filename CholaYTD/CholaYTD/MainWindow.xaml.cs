using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CliWrap;
using Tyrrrz.Extensions;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
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
        private List<string> failedDownloads = new List<string>();

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
            MessageBoxResult dialogConfirm = System.Windows.MessageBox.Show("¿Confirma que quiere comenzar el proceso de descargas?", "Confirmación de descarga", System.Windows.MessageBoxButton.YesNo);
            if (dialogConfirm == MessageBoxResult.Yes)
            {
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
                foreach (ListBoxItem item in listaEnlaces.Items)
                {
                    tempList.Add((string)item.ToolTip);
                }
                args = tempList.ToArray();

                // chekeamos el tipo de descarga elegida en el radioButton (video normal, solo video, solo audio)
                if ((bool)rb_normal.IsChecked)
                {
                    DistribuidorTipoDescargas(args);
                }
                else if ((bool)rb_audio.IsChecked)
                {
                    DistribuidorTipoDescargas(args);
                }
                else if ((bool)rb_video.IsChecked)
                {
                    DistribuidorTipoDescargas(args);
                }
            }
        }

        private async Task descargarVideoHDAsync(string id)
        {
            numVideoProgress++;
            // Get video info
            try
            {
                updateProgressBar(11, "Video #" + numVideoProgress + " - Recogiendo información del video...");
                var video = await YoutubeClient.GetVideoAsync(id);
                updateProgressBar(13, "Video #" + numVideoProgress + " - ...información de video recogida.");
                updateProgressBar(13, "Video #" + numVideoProgress + " - Renombrando...");
                var cleanTitle = video.Title.Replace(Path.GetInvalidFileNameChars(), '_');
                updateProgressBar(15, "Video #" + numVideoProgress + " - ... video renombrado.");

                // Get best streams
                var streamInfoSet = await YoutubeClient.GetVideoMediaStreamInfosAsync(id);
                updateProgressBar(17, "Video #" + numVideoProgress + " - Buscando mejor stream...");
                var videoStreamInfo = streamInfoSet.Video.WithHighestVideoQuality();
                updateProgressBar(19, "Video #" + numVideoProgress + " - Buscando la mayor calidad...");
                var audioStreamInfo = streamInfoSet.Audio.WithHighestBitrate();
                updateProgressBar(21, "Video #" + numVideoProgress + " - Buscando el mejor sonido...");

                // Download streams
                updateProgressBar(22, "Video #" + numVideoProgress + " - Creando directorio temporal...");
                Directory.CreateDirectory(TempDirectoryPath);
                updateProgressBar(23, "Video #" + numVideoProgress + " - Extrayendo extensión de archivos...");
                var videoStreamFileExt = videoStreamInfo.Container.GetFileExtension();
                updateProgressBar(24, "Video #" + numVideoProgress + " - Creando rutas de archivos...");
                var videoStreamFilePath = Path.Combine(TempDirectoryPath, $"VID-{Guid.NewGuid()}.{videoStreamFileExt}");
                updateProgressBar(25, "Video #" + numVideoProgress + " - Descargando el video...");
                await YoutubeClient.DownloadMediaStreamAsync(videoStreamInfo, videoStreamFilePath);
                updateProgressBar(36, "Video #" + numVideoProgress + " - ... video descargado.");
                var audioStreamFileExt = audioStreamInfo.Container.GetFileExtension();
                var audioStreamFilePath = Path.Combine(TempDirectoryPath, $"AUD-{Guid.NewGuid()}.{audioStreamFileExt}");
                updateProgressBar(39, "Video #" + numVideoProgress + " - Descargando el sonido...");
                await YoutubeClient.DownloadMediaStreamAsync(audioStreamInfo, audioStreamFilePath);
                updateProgressBar(49, "Video #" + numVideoProgress + " - ...sonido descargado.");

                // Mux streams
                updateProgressBar(50, "Video #" + numVideoProgress + " - Creando directorio de salida...");
                Directory.CreateDirectory(OutputDirectoryPath);
                var outputFilePath = Path.Combine(OutputDirectoryPath, $"{cleanTitle}.mp4");
                updateProgressBar(60, "Video #" + numVideoProgress + " - Combinando video y audio...");
                await FfmpegCli.ExecuteAsync($"-i \"{videoStreamFilePath}\" -i \"{audioStreamFilePath}\" -shortest \"{outputFilePath}\" -y");
                updateProgressBar(98, "Video #" + numVideoProgress + " - ... video y audio combinados con éxito.");

                // Delete temp files
                updateProgressBar(99, "Video #" + numVideoProgress + " - Eliminando archivos temporales...");
                File.Delete(videoStreamFilePath);
                File.Delete(audioStreamFilePath);
                updateProgressBar(100, "Video #" + numVideoProgress + " - ... proceso finalizado.");
            } catch (VideoUnavailableException ex)
            {
                Console.WriteLine("Excepción capturada: " + ex.Message);
                Console.WriteLine("Video NO DISPONIBLE: https://www.youtube.com/watch?v=" + id);
                failedDownloads.Add(id);
            }
        }

        private async Task DescargarSoloAudioMP3(string id)
        {
            numVideoProgress++;
            // Get video info
            updateProgressBar(13, "Video #" + numVideoProgress + " - Recogiendo información del video...");
            try
            {
                var video = await YoutubeClient.GetVideoAsync(id);
                var set = await YoutubeClient.GetVideoMediaStreamInfosAsync(id);
                updateProgressBar(16, "Video #" + numVideoProgress + " - Extrayendo datos...");
                var cleanTitle = video.Title.Replace(Path.GetInvalidFileNameChars(), '_');

                // Get highest bitrate audio-only or highest quality mixed stream
                updateProgressBar(19, "Video #" + numVideoProgress + " - Buscando stream de máxima calidad...");
                var streamInfo = GetBestAudioStreamInfo(set);

                // Download to temp file
                updateProgressBar(25, "Video #" + numVideoProgress + " - Descargando archivo temporal...");
                Directory.CreateDirectory(TempDirectoryPath);
                var streamFileExt = streamInfo.Container.GetFileExtension();
                var streamFilePath = Path.Combine(TempDirectoryPath, $"{Guid.NewGuid()}.{streamFileExt}");
                updateProgressBar(40, "Video #" + numVideoProgress + " - Descargando archivo temporal...");
                await YoutubeClient.DownloadMediaStreamAsync(streamInfo, streamFilePath);

                // Convert to mp3
                Directory.CreateDirectory(OutputDirectoryPath);
                updateProgressBar(50, "Video #" + numVideoProgress + " - Guardando el archivo...");
                var outputFilePath = Path.Combine(OutputDirectoryPath, $"{cleanTitle}.mp3");
                updateProgressBar(75, "Video #" + numVideoProgress + " - Codificando archivo temporal a MP3...");
                await FfmpegCli.ExecuteAsync($"-i \"{streamFilePath}\" -q:a 0 -map a \"{outputFilePath}\" -y");

                // Delete temp file
                File.Delete(streamFilePath);
                updateProgressBar(100, "Video #" + numVideoProgress + " - ... proceso finalizado.");
            } catch (VideoUnavailableException ex)
            {
                Console.WriteLine("Excepcion capturada: " + ex.Message);
                Console.WriteLine("Video NO DISPONIBLE: https://www.youtube.com/watch?v=" + id);
                failedDownloads.Add(id);
            }
            // Edit mp3 metadata
            //Console.WriteLine("Writing metadata...");
            //var idMatch = Regex.Match(video.Title, @"^(?<artist>.*?)-(?<title>.*?)$");
            //var artist = idMatch.Groups["artist"].Value.Trim();
            //var title = idMatch.Groups["title"].Value.Trim();
            //using (var meta = TagLib.File.Create(outputFilePath))
            //{
            //    meta.Tag.Performers = new[] { artist };
            //    meta.Tag.Title = title;
            //    meta.Save();
            //}

            //Console.WriteLine($"Downloaded and converted video [{id}] to [{outputFilePath}]");
        }

        private async Task DescargarVideoSinAudioAsync(string id)
        {
            numVideoProgress++;
            // Get video info
            try
            {
                updateProgressBar(11, "Video #" + numVideoProgress + " - Recogiendo información del video...");
                var video = await YoutubeClient.GetVideoAsync(id);
                updateProgressBar(13, "Video #" + numVideoProgress + " - ...información de video recogida.");
                updateProgressBar(13, "Video #" + numVideoProgress + " - Renombrando...");
                var cleanTitle = video.Title.Replace(Path.GetInvalidFileNameChars(), '_');
                updateProgressBar(15, "Video #" + numVideoProgress + " - ... video renombrado.");

                // Get best streams
                var streamInfoSet = await YoutubeClient.GetVideoMediaStreamInfosAsync(id);
                updateProgressBar(17, "Video #" + numVideoProgress + " - Buscando mejor stream...");
                var videoStreamInfo = streamInfoSet.Video.WithHighestVideoQuality();
                updateProgressBar(19, "Video #" + numVideoProgress + " - Buscando la mayor calidad...");
                var audioStreamInfo = streamInfoSet.Audio.WithHighestBitrate();
                updateProgressBar(21, "Video #" + numVideoProgress + " - Buscando el mejor sonido...");

                // Download streams
                updateProgressBar(22, "Video #" + numVideoProgress + " - Creando directorio temporal...");
                Directory.CreateDirectory(TempDirectoryPath);
                updateProgressBar(23, "Video #" + numVideoProgress + " - Extrayendo extensión de archivos...");
                var videoStreamFileExt = videoStreamInfo.Container.GetFileExtension();
                updateProgressBar(24, "Video #" + numVideoProgress + " - Creando rutas de archivos...");
                var videoStreamFilePath = Path.Combine(TempDirectoryPath, $"VID-{Guid.NewGuid()}.{videoStreamFileExt}");
                updateProgressBar(25, "Video #" + numVideoProgress + " - Descargando el video...");
                await YoutubeClient.DownloadMediaStreamAsync(videoStreamInfo, videoStreamFilePath);
                updateProgressBar(36, "Video #" + numVideoProgress + " - ... video descargado.");
                //var audioStreamFileExt = audioStreamInfo.Container.GetFileExtension();
                //var audioStreamFilePath = Path.Combine(TempDirectoryPath, $"AUD-{Guid.NewGuid()}.{audioStreamFileExt}");
                //updateProgressBar(39, "Video #" + numVideoProgress + " - Descargando el sonido...");
                //await YoutubeClient.DownloadMediaStreamAsync(audioStreamInfo, audioStreamFilePath);
                //updateProgressBar(49, "Video #" + numVideoProgress + " - ...sonido descargado.");
                
                // Mux streams
                updateProgressBar(50, "Video #" + numVideoProgress + " - Creando directorio de salida...");
                Directory.CreateDirectory(OutputDirectoryPath);
                var outputFilePath = Path.Combine(OutputDirectoryPath, $"{cleanTitle}.mp4");
                File.Move(videoStreamFilePath, outputFilePath);
                updateProgressBar(75, "Video #" + numVideoProgress + " - Terminando, y moviendo archivo...");
                //await FfmpegCli.ExecuteAsync($"-i \"{videoStreamFilePath}\" -i \"{audioStreamFilePath}\" -shortest \"{outputFilePath}\" -y");
                //updateProgressBar(98, "Video #" + numVideoProgress + " - ... video y audio combinados con éxito.");

                // Delete temp files
                updateProgressBar(99, "Video #" + numVideoProgress + " - Eliminando archivos temporales...");
                File.Delete(videoStreamFilePath);
                //File.Delete(audioStreamFilePath);
                updateProgressBar(100, "Video #" + numVideoProgress + " - ... proceso finalizado.");
            }
            catch (VideoUnavailableException ex)
            {
                // EXCEPCION VideoUnavailableException (video no disponible)
                Console.WriteLine("Excepción capturada: " + ex.Message);
                Console.WriteLine("Video NO DISPONIBLE: https://www.youtube.com/watch?v=" + id);
                failedDownloads.Add(id);
            }
        }

        private static MediaStreamInfo GetBestAudioStreamInfo(MediaStreamInfoSet set)
        {
            if (set.Audio.Any())
                return set.Audio.WithHighestBitrate();
            if (set.Muxed.Any())
                return set.Muxed.WithHighestVideoQuality();
            throw new Exception("No applicable media streams found for this video");
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
                if ((bool)rb_normal.IsChecked)
                {
                    await descargarVideoHDAsync(video.Id);
                }
                else if ((bool)rb_audio.IsChecked)
                {
                    await DescargarSoloAudioMP3(video.Id);
                }
                else if ((bool)rb_video.IsChecked)
                {
                    await DescargarVideoSinAudioAsync(video.Id);
                }                
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
                    if ((bool)rb_normal.IsChecked)
                    {
                        await descargarVideoHDAsync(arg);
                    }
                    else if ((bool)rb_audio.IsChecked)
                    {
                        await DescargarSoloAudioMP3(arg);
                    }
                    else if ((bool)rb_video.IsChecked)
                    {
                        await DescargarVideoSinAudioAsync(arg);
                    }
                }

                // Video URL
                else if ( YoutubeClient.TryParseVideoId ( arg, out string videoId ) )
                {
                    updateProgressBar ( 5, "Determinando tipo de enlace..." );
                    if ((bool)rb_normal.IsChecked)
                    {
                        await descargarVideoHDAsync(videoId);
                    }
                    else if ((bool)rb_audio.IsChecked)
                    {
                        await DescargarSoloAudioMP3(videoId);
                    }
                    else if ((bool)rb_video.IsChecked)
                    {
                        await DescargarVideoSinAudioAsync(videoId);
                    }                    
                }

                // Unknown
                else
                {
                    updateProgressBar ( 0, "Error: el enlace no parece válido." );
                    throw new ArgumentException ( $"Unrecognized URL or ID: [{arg}]", nameof ( arg ) );
                }
            }
            updateProgressBar (0, "Todas las tareas finalizadas." );
            //Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\Output");
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
                listaEnlaces.Items.Clear();
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

                if (failedDownloads.Any())
                {
                    WpfMBFin dialogoPrueba = new WpfMBFin(failedDownloads);
                    hacerPantallaBorrosa();
                    dialogoPrueba.ShowDialog();
                    deshacerPantallaBorrosa();
                    Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\Output");
                }
                else
                {
                    Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\Output");
                }                
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

        //private async void descargarSoloAudio(string enlaceAlVideo)
        //{
        //    //string link = "https://www.youtube.com/watch?v=fJ9rUzIMcZQ";
        //    var url = enlaceAlVideo;
        //    var id = YoutubeClient.ParseVideoId ( url );

        //    var client = new YoutubeClient ();
        //    var streamInfoSet = await client.GetVideoMediaStreamInfosAsync ( id );
        //    var streamInfo = streamInfoSet.Audio.WithHighestBitrate ();
        //    var ext = streamInfo.Container.GetFileExtension ();
        //    await client.DownloadMediaStreamAsync ( streamInfo, $"downloaded_video.{ext}" );
        //}

        private void tB_introEnlace_GotFocus(object sender, RoutedEventArgs e)
        {
            tB_introEnlace.Text = "";
            tB_introEnlace.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF495D7A"));
            tB_introEnlace.FontWeight = FontWeights.Normal;
            tB_introEnlace.FontStyle = FontStyles.Normal;
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
                        tB_introEnlace.FontStyle = FontStyles.Italic;
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
                tB_introEnlace.FontStyle = FontStyles.Italic;
                tB_introEnlace.Text = "Introduzca un enlace de un video o una lista de reproducción de Youtube...";
            }
        }

        private async Task RecogerInfoVideoLista(string tb_url)
        {
            //Console.WriteLine(YoutubeClient.ValidateVideoId(tb_url.Substring(tb_url.IndexOf('=') + 1)));
            try
            {
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
            } catch (VideoUnavailableException ex)
            {

                // EXCEPCION VideoUnavailableException (video no disponible)

                tB_introEnlace.Text = "EL VIDEO QUE HA INTRODUCIDO NO ESTÁ DISPONIBLE";
                tB_introEnlace.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC55353"));
                tB_introEnlace.FontWeight = FontWeights.Bold;
                Console.WriteLine("El video con URL: " + tb_url + ", no se encuentra disponible.");
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
            tB_introEnlace.FontStyle = FontStyles.Italic;
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

        private void hacerPantallaBorrosa()
        {
            //hacemos la pantalla borrosa
            System.Windows.Media.Effects.BlurEffect objBlur = new System.Windows.Media.Effects.BlurEffect();
            objBlur.Radius = 4;
            this.Effect = objBlur;
        }

        private void deshacerPantallaBorrosa()
        {
            // eliminamos pantalla borrosa cuando el dialogo se cierra
            this.Effect = null;
        }
    }
}
