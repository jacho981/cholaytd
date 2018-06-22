using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using CliWrap;
using Tyrrrz.Extensions;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;

namespace CholaYTD
{
    public class YoutubeDownloader
    {
        private static readonly YoutubeClient YoutubeClient = new YoutubeClient ();
        private static readonly Cli FfmpegCli = new Cli ( "ffmpeg.exe" );

        private static readonly string TempDirectoryPath = Path.Combine ( Directory.GetCurrentDirectory (), "Temp" );
        private static readonly string OutputDirectoryPath = Path.Combine ( Directory.GetCurrentDirectory (), "Output" );

        private static async Task DownloadVideoAsync( string id )
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

        private static async Task DownloadPlaylistAsync( string id )
        {
            Console.WriteLine ( $"Working on playlist [{id}]..." );

            // Get playlist info
            var playlist = await YoutubeClient.GetPlaylistAsync ( id );
            Console.WriteLine ( $"{playlist.Title} ({playlist.Videos.Count} videos)" );

            // Work on the videos
            Console.WriteLine ();
            foreach ( var video in playlist.Videos )
            {
                await DownloadVideoAsync ( video.Id );
                Console.WriteLine ();
            }
        }

        public static async Task MainAsync( string[] args )
        {
            foreach ( var arg in args )
            {
                // Try to determine the type of the URL/ID that was given

                // Playlist ID
                if ( YoutubeClient.ValidatePlaylistId ( arg ) )
                {
                    await DownloadPlaylistAsync ( arg );
                }

                // Playlist URL
                else if ( YoutubeClient.TryParsePlaylistId ( arg, out string playlistId ) )
                {
                    await DownloadPlaylistAsync ( playlistId );
                }

                // Video ID
                else if ( YoutubeClient.ValidateVideoId ( arg ) )
                {
                    await DownloadVideoAsync ( arg );
                }

                // Video URL
                else if ( YoutubeClient.TryParseVideoId ( arg, out string videoId ) )
                {
                    await DownloadVideoAsync ( videoId );
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
    }
}