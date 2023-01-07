using System.Security.Cryptography;

namespace CLIDownloader;

internal static class Validator
{

   public static async Task Validate(IEnumerable<DownloadData> downloads, string directory)
   {
      using var sha1 = SHA1.Create();
      using var sha256 = SHA256.Create();

      foreach (var download in downloads)
      {
         Console.WriteLine();
         Console.ForegroundColor = ConsoleColor.White;
         Console.WriteLine(download.File);
         var file = new FileInfo(Path.Combine(directory, download.File));
         if (!file.Exists)
         {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("File does not exist!");
            continue;
         }
         if (download is { SHA1: null, SHA256: null })
         {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No hash found!");
         }

         using var stream = file.OpenRead();

         var validateHash = (HashAlgorithm algorithm, string? hash, string hashType) =>
         {
            if (hash is not null)
            {
               stream.Position = 0;
               var algorithmHash = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLower();
               if (hash.Equals(algorithmHash))
               {
                  Console.ForegroundColor = ConsoleColor.Green;
                  Console.WriteLine($"Valid {hashType}");
               }
               else
               {
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.WriteLine($"Invalid {hashType}");
               }
            }
         };
         validateHash(sha1, download.SHA1, "SHA1");
         validateHash(sha256, download.SHA256, "SHA256");
      }
      Console.ResetColor();
   }
}
