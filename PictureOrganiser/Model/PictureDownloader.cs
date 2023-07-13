using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PictureOrganiser.Program;

namespace PictureOrganiser.Model
{
    class PictureDownloader
    {
        static readonly HttpClient Client = new HttpClient();

        public event DownloadComplete OnDownloadComplete;

        public SemaphoreSlim Semaphore;
        private int DownloadedOrder { get; set; }

        public async Task Start(Configuration config, CancellationToken stoppingToken)
        {
            var url = "https://picsum.photos/200/300";
            ServicePointManager.FindServicePoint(new Uri(url)).ConnectionLimit = config.Parallelism;
            Semaphore = new SemaphoreSlim(config.Parallelism);

            if (!Directory.Exists(config.SavePath))
            {
                Directory.CreateDirectory(config.SavePath);
            }

            System.Console.WriteLine("$$$");
            System.Console.WriteLine($"Downloading {config.Count} images ({config.Parallelism} parallel downloads at most)");
            System.Console.WriteLine("");

            var downloadTasks = new List<Task>();
            var remaining = config.Count;

            DownloadedOrder = 0;

            while (remaining > 0 && stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    remaining--;

                    await Semaphore.WaitAsync();

                    downloadTasks.Add(DownLoadImage(url, config));
                }
                catch (TaskCanceledException e)
                {
                    //timeout => retry
                }
                catch (OperationCanceledException e)
                {
                    //timeout => retry
                }
                catch (Exception e)
                {
                    //Semaphore.Disposed && stoppingToken.IsCancellationRequested => exit
                }

            }

            Task.WaitAll(downloadTasks.ToArray());
        }

        private async Task DownLoadImage(string url, Configuration config)
        {
            using (HttpResponseMessage response = await Client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                var imageBytes = await response.Content.ReadAsByteArrayAsync();

                var pictures = Directory.GetFiles(config.SavePath).Where(f => f.EndsWith(".png") && int.TryParse(f.Split('/').Last().Replace(".png", ""), out var fileName) && fileName > 0 && fileName < DownloadedOrder);
                var hasSameImage = pictures.Any(picture =>
                {
                    var existingImageBytes = File.ReadAllBytes(picture);
                    return imageBytes.Length == existingImageBytes.Length && imageBytes.SequenceEqual(existingImageBytes);
                });

                if (!hasSameImage)
                {
                    File.WriteAllBytes($"{config.SavePath}/{++DownloadedOrder}.png", imageBytes);

                    OnDownloadComplete?.Invoke(DownloadedOrder, config.Count);

                    Semaphore.Release();
                }
                else
                {
                    // If any same image exists, do not increase DownloadedOrder
                    await DownLoadImage(url, config);
                    return;
                }
            }
        }
    }
}
