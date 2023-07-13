using Newtonsoft.Json;
using PictureOrganiser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PictureOrganiser
{
    class Program
    {

        public delegate void DownloadComplete(int downloadedImageCount, int totalCount);

        static void Main(string[] args)
        {
            System.Console.WriteLine("Welcome to Picture Organiser!");
            System.Console.WriteLine("Select configuration type :");

            var configurationType = "unknown";

            while (configurationType != "c" && configurationType != "j")
            {
                System.Console.WriteLine("$ Press C to continue with Console or Press J to continue with Json");
                System.Console.Write("< ");
                configurationType = System.Console.ReadLine().ToLower();
            }

            Configuration config = null;
            if (configurationType == "c")
            {
                config = GetConsoleConfiguration();
            }
            else if (configurationType == "j")
            {
                config = GetJsonConfiguration("Input.json");
            }

            var downloader = new PictureDownloader();
            var stoppingSource = new CancellationTokenSource();
            var stoppingToken = stoppingSource.Token;

            downloader.OnDownloadComplete += (int downloadedImageCount, int totalCount) =>
            {
                System.Console.CursorLeft = 0;
                System.Console.Write($"Progress: {downloadedImageCount}/{totalCount}");
            };

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                try
                {
                    e.Cancel = true;
                    stoppingSource.Cancel();
                    downloader.Semaphore.Dispose();

                    //Want to delete only downloaded images in this process. Keep others
                    var pictures = Directory.GetFiles(config.SavePath).Where(f => f.EndsWith(".png") && int.TryParse(f.Split('\\').Last().Replace(".png", ""), out var fileName) && fileName > 0 && fileName <= config.Count);
                    foreach (var picture in pictures)
                    {
                        File.Delete(picture);
                    }
                }
                catch (Exception ex)
                {
                    var d = 0;
                }
            };

            var downloadTask = downloader.Start(config, stoppingToken);

            Task.WaitAll(downloadTask);
            System.Console.WriteLine("");
            System.Console.WriteLine("$$$");
            System.Console.WriteLine("Download Completed! Press any key to exit.");
            System.Console.ReadLine();
        }

        static Configuration GetConsoleConfiguration()
        {
            Configuration config = new Configuration();

            var consoleRead = "unknown";
            while (!int.TryParse(consoleRead, out config.Count) || config.Count == 0)
            {
                consoleRead = RequestConsoleData("Enter the number of images to download:");
            }
            consoleRead = "unknown";
            while (!int.TryParse(consoleRead, out config.Parallelism) || config.Parallelism == 0)
            {
                consoleRead = RequestConsoleData("Enter the maximum parallel download limit:");
            }
            consoleRead = RequestConsoleData("Enter the save path (default: ./outputs)");
            if (!string.IsNullOrEmpty(consoleRead))
            {
                config.SavePath = consoleRead;
            }
            else
            {
                config.SavePath = "./outputs";
            }

            return config;
        }

        static Configuration GetJsonConfiguration(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                var serializer = new JsonSerializer();

                using (var sr = new StreamReader(stream))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    return serializer.Deserialize<Configuration>(jsonTextReader);
                }
            };
        }

        static string RequestConsoleData(string question)
        {
            System.Console.WriteLine($"$ {question}");
            System.Console.Write("< ");
            return System.Console.ReadLine();
        }

        static void Start()
        {

        }
    }
}
