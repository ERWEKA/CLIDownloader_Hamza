using System.CommandLine;
using System.Security.Cryptography;
using System.Text;

using CLIDownloader;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var rootCommand = new RootCommand("Downloads multiple files from various sources in parallel");

// Set up "download [--verbose] [--dry-run] [parallel-downloads=N] config.yml" command
var verboseOption = new Option<bool>("--verbose");

var dryRunOption = new Option<bool>("--dry-run", "Checks if the config is correctly parsed");

var parallelDownloadsOption = new Option<int>("parallel-downloads", "Sets parallelism degree");

var configArgument = new Argument<string>(
   "configuration-file",
   "Configuration file (.yaml) which contains all necessary data needed for the download job");

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
var validateCommand = new Command(
   "validate",
   "Validates downloaded files integrity against their respective checksum when available")
{
   verboseOption,
   configArgument
};

validateCommand.SetHandler(
   async (isVerbose, configFile) =>
   {
      await ExecuteValidateCommand(isVerbose, configFile);
   },
   verboseOption, configArgument);

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
   var directory = Directory.CreateDirectory(conf.GetValue<string>("config:download_dir")!);

   var downloads = conf.GetRequiredSection("downloads").Get<IEnumerable<DownloadData>>()!;

   if (isDryRun)
   {
      ExecuteDownloadDryRun(downloads, parallelDownloads, directory);
      return;
   }

   var downloader = host.Services.GetRequiredService<Downloader>();
   await downloader.StartDownloadsAsync(downloads, parallelDownloads, directory);
}

static void ExecuteDownloadDryRun(IEnumerable<DownloadData> downloads, int parallelDownloads, DirectoryInfo directory)
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

static async Task ExecuteValidateCommand(bool isVerbose, string configFile)
{
   // The builder checks if the config file exists, and throws an exception accordingly
   // TODO: Parse config file manually
   IHost host = Host.CreateDefaultBuilder()
      .ConfigureAppConfiguration(builder =>
      {
         builder.AddYamlFile(configFile, optional: false);
      })
      .Build();

   // Read configuration
   var conf = host.Services.GetRequiredService<IConfiguration>();
   var directory = conf.GetValue<string>("config:download_dir")!;
   if (!Directory.Exists(directory))
   {
      Console.WriteLine($"Download directory {directory} does not exist!");
      return;
   }

   var downloads = conf.GetRequiredSection("downloads").Get<IEnumerable<DownloadData>>()!;

   await Validator.Validate(downloads, directory);
}