using System;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

namespace CLIDownloader;

public class Downloader
{
   readonly static string progressBarChar = Char.ConvertFromUtf32(0x2588);

   readonly HttpClient httpClient;
   readonly ILogger<Downloader> logger;

   public Downloader(HttpClient client, ILogger<Downloader> logger)
   {
      httpClient = client;
      this.logger = logger;
   }

   internal async Task StartDownloadsAsync(IEnumerable<DownloadData> downloads, int parallelDownloads, DirectoryInfo directory)
   {
      logger.LogInformation("Downloading");
      var consoleLock = new object();

      // Between setting the cursor posotion and writing the actual message a race condition takes place,
      // with unpredictabel results, so the operation has to be made atomic, hence the lock
      var writeToConsole = (int left, int top, string message) =>
      {
         lock (consoleLock)
         {
            Console.SetCursorPosition(left, top);
            Console.Write(message);
         }
      };
      Console.WriteLine();
      Console.WriteLine($"Download folder: {directory.FullName}");
      Console.WriteLine($"Parallel downloads: {parallelDownloads}");

      // Reserve a placeholder for the visual progress of each download
      var visualIndex = Console.CursorTop / 2;
      await Parallel.ForEachAsync(downloads, new ParallelOptions { MaxDegreeOfParallelism = parallelDownloads }, async (download, token) =>
      {
         // Interlocked solves the race condition while incrementing the index
         var index = Interlocked.Increment(ref visualIndex) * 2;

         var logIndex = index;
         logger.LogDebug("{0} - Downloading file {1} from source {2}", logIndex, download.File, download.URL);

         // Set up visual progress
         writeToConsole(0, index, $"{download.File} - Overwrite: {download.Overwrite is true}");
         index += 1;

         var file = new FileInfo(Path.Combine(directory.FullName, download.File));
         if (file.Exists && download.Overwrite is false)
         {
            writeToConsole(0, index, "File exists !");
            logger.LogTrace("{0} - Download skipped because file exists and overwrite option is false", logIndex);
            logger.LogDebug("{0} - Downloaded file - Skipped", logIndex);

            return;
         }
         writeToConsole(101, index, "| 100 %");

         // Visual progress bar length
         var progressBar = 0;         

         // I used IProgress<T>, because it is recommended and does report progress in a non-blocking/asynchronous fashion
         // but during tests found out that in a non-UI context there is no guarantee that it will report the progress in the original order
         // In other words, while all progress will be reported, 7% maybe reported before 3%
         // In a UI-context the order is guaranteed
         var reportProgress = (int p) =>
         {
            logger.LogTrace("{0} - Progress: {1}", logIndex, p);

            // In case of a smaller file than bufferSize * 100 in this case: 128Kb * 100 = 12.8 Mb
            // The visual progress bar won't be filled to 100 % otherwise
            while (progressBar <= p)
            {
               writeToConsole(progressBar++, index, progressBarChar);
            }
         };

         var stopWatch = Stopwatch.StartNew();
         await DownloadAsync(download, reportProgress, file, logIndex);
         stopWatch.Stop();

         writeToConsole(111, index, $"({stopWatch.Elapsed.TotalSeconds:N2} s)");
         logger.LogDebug("{0} - Downloaded file in {1:N2} s", logIndex, stopWatch.Elapsed.TotalSeconds);
      });

      // the cursor will be automatically set after the last download to finish,
      // instead of the last download to start, in which case other downloads progress will be overwritten
      // Manually set cursor position after the last download to start
      Console.SetCursorPosition(0, (visualIndex + 1) * 2);
      logger.LogInformation("Downloaded");
   }

   /// <param name="bufferSize">Tests needs to be ran to determine the optimal buffer size
   /// for both the Http stream as well as the file stream</param>
   async Task DownloadAsync(DownloadData download, Action<int> reportProgress, FileInfo fileInfo, int logIndex, int bufferSize = 128 * 1024)
   {
      logger.LogTrace("{0} - Sending request", logIndex);

      // Request URL
      using var response = await httpClient.GetAsync(download.URL, HttpCompletionOption.ResponseHeadersRead);

      // Convert ContentLength to Kb, in order to stay withing the int range and simplify the use case
      var contentLength = response.Content.Headers.ContentLength ?? 0;
      logger.LogTrace("{0} - Content length {1} bytes", logIndex, contentLength);

      using var stream = await response.Content.ReadAsStreamAsync();

      using var file = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);

      logger.LogTrace("{0} - Copying stream", logIndex);
      await CopyStreamAsync(stream, file, contentLength, reportProgress, bufferSize);
      logger.LogTrace("{0} - Copied stream", logIndex);
   }

   public static async Task CopyStreamAsync(Stream source, Stream destination, long size, Action<int> reportProgress, int bufferSize)
   {
      var buffer = new Memory<byte>(new byte[bufferSize]);
      long bytesRead = 0;
      var downloadProgress = -1;
      int length;

      // The actual download process
      while ((length = await source.ReadAsync(buffer)) > 0)
      {
         await destination.WriteAsync(buffer[..length]);
         bytesRead += length;

         // Added the check in order to avoid reporting the progress 1000s of times, when maximum 100 steps are relevant
         if (downloadProgress < bytesRead * 100 / size)
         {
            downloadProgress = (int)(bytesRead * 100 / size);
            reportProgress(downloadProgress);
         }
      }
   }
}