﻿using System.CommandLine;

using CLIDownloader;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var rootCommand = new RootCommand("Downloads multiple files from various sources in parallel");

// Set up "download [--verbose] [--dry-run] [parallel-downloads=N] config.yml" command
var verboseOption = new Option<bool>("--verbose");

var dryRunOption = new Option<bool>("--dry-run");

var parallelDownloadsOption = new Option<int>
    (name: "parallel-downloads",
    description: "An option whose argument is parsed as an int.");

var configArgument = new Argument<string>(
   name: "configuration-file", "Configuration file (.yaml) which contains all necessary data needed for the download job");

var downloadCommand = new Command("download", "Downloads files from urls")
{
   verboseOption,
   dryRunOption,
   parallelDownloadsOption,
   configArgument
};

downloadCommand.SetHandler(
   async (isVerbose, isDryRun, parallelDownloads, configFile) =>
   {
      await ExecuteDownloadCommand(isVerbose, isDryRun, parallelDownloads, configFile);
   },
   verboseOption, dryRunOption, parallelDownloadsOption, configArgument);

rootCommand.Add(downloadCommand);

// Set up "validate [--verbose] config.yml" command
var validateCommand = new Command("validate", "Validates downloaded files integrity against their respective checksum if available")
{
   verboseOption,
   configArgument
};

rootCommand.Add(validateCommand);
await rootCommand.InvokeAsync(args);


static async Task ExecuteDownloadCommand(bool isVerbose, bool isDryRun, int parallelDownloads, string configFile)
{
   // The builder checks if the config file exists, and throws an exception accordingly
   IHost host = Host.CreateDefaultBuilder()
      .ConfigureAppConfiguration(builder =>
      {
         builder.AddYamlFile(configFile, optional: false);
      })
      .ConfigureServices(services =>
      {
         services.AddSingleton<HttpClient>();
         services.AddSingleton<Downloader>();
      })
      .Build();

   // Read configuration
   var conf = host.Services.GetService<IConfiguration>()!;
   if (parallelDownloads == 0)
   {
      parallelDownloads = conf.GetValue<int>("config:parallel_downloads");
   }
   var downloadDirectory = conf.GetValue<string>("config:download_dir")!;
   var directory = Directory.CreateDirectory(conf.GetValue<string>("config:download_dir")!);

   var downloads = new List<DownloadData>();
   conf.GetRequiredSection("downloads").Bind(downloads);

   await host.StartAsync();
   if (isDryRun)
   {
      ExecuteDryRun(downloads, parallelDownloads, directory);
      return;
   }

   var downloader = host.Services.GetRequiredService<Downloader>();
   await downloader.StartDownloadsAsync(downloads, parallelDownloads, directory);
}

static void ExecuteDryRun(IEnumerable<DownloadData> downloads, int parallelDownloads, DirectoryInfo directory)
{
   Console.WriteLine();
   Console.WriteLine($"Download folder: {directory.FullName}");
   Console.WriteLine($"Parallel downloads: {parallelDownloads}");
   Console.WriteLine("Downloads:");

   foreach (var download in downloads)
   {
        Console.WriteLine();
        Console.WriteLine(download);
    }
}