using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CLIDownloader.Tests;

[TestClass()]
public class DownloaderTests
{

   [TestMethod()]
   [DataRow(549, 4, 101)]
   [DataRow(549, 16, 35)]
   [DataRow(1987, 32, 63)]
   public async Task CopyStreamAsyncTest(int streamSize, int bufferSize, int expectedReportsCount)
   {
      var sourceBytes = new byte[streamSize];
      var rnd = new Random();
      rnd.NextBytes(sourceBytes);

      using var source = new MemoryStream(sourceBytes);
      using var destination = new MemoryStream();

      var reportsCount = 0;
      var finalProgress = 0;
      var reportProgress = (int p) =>
      {
         reportsCount++;
         finalProgress = p;
      };

      await Downloader.CopyStreamAsync(source, destination, streamSize, reportProgress, bufferSize);

      // Makes sure progress is reportd accordingly
      Assert.AreEqual(expectedReportsCount, reportsCount);
      Assert.AreEqual(100, finalProgress);

      var destinationBytes = destination.ToArray();
      CollectionAssert.AreEqual(sourceBytes, destinationBytes);

      destinationBytes[0] = Convert.ToByte((destinationBytes[0] + 1) % 256);
      CollectionAssert.AreNotEqual(sourceBytes, destinationBytes);
   }
}