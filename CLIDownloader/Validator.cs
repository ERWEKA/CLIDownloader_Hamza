using System.Diagnostics.Eventing.Reader;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace CLIDownloader;

internal class Validator
{
   private readonly ILogger logger;

   public Validator(ILogger<Validator> logger)
   {
      this.logger = logger;
   }

   public async Task Validate(IEnumerable<DownloadData> downloads, string directory)
   {
      logger.LogInformation("Validating");

      using var sha1 = SHA1.Create();
      using var sha256 = SHA256.Create();

      foreach (var download in downloads)
      {
         logger.LogDebug("Validating file {0}", download.File);

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
            logger.LogDebug("No hash found!", download.File);
         }

         using var stream = file.OpenRead();

         var validateHash = (HashAlgorithm algorithm, string? hash, string hashType) =>
         {
            if (hash is not null)
            {
               logger.LogDebug("\tValidating {0}", hashType);
               stream.Position = 0;
               var algorithmHash = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLower();
               if (hash.Equals(algorithmHash))
               {
                  Console.ForegroundColor = ConsoleColor.Green;
                  Console.WriteLine($"Valid {hashType}");
                  logger.LogDebug("\t\tHash match");
                  logger.LogTrace("\t\tHash: {0}", hash);
               }
               else
               {
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.WriteLine($"\t\tInvalid {hashType}");

                  logger.LogDebug("\t\tHash mismatch");
                  logger.LogTrace("\t\tConfig hash: {0}", hash);
                  logger.LogTrace("\t\tComputed hash: {1}", algorithmHash);

               }
               logger.LogDebug("\tValidated {0}", hashType);
            }
         };
         validateHash(sha1, download.SHA1, "SHA1");
         validateHash(sha256, download.SHA256, "SHA256");
         logger.LogDebug("Validated file");

      }
      Console.ResetColor();
      logger.LogInformation("Validated");
   }
}
