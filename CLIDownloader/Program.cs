using System.CommandLine;

var rootCommand = new RootCommand("Downloads multiple files from various sources in parallel");

var verboseOption = new Option<bool>("--verbose");

var dryRunOption = new Option<bool>("--dry-run");

var parallelDownloadsOption = new Option<int>
    (name: "parallel-downloads",
    description: "An option whose argument is parsed as an int.");

var configArgument = new Argument<string>(
   name: "configuration-file", "Configuration file (.yaml) which contains all necessary data you need for the download job");

var downloadCommand = new Command("download", "Downloads files from urls")
{
   verboseOption,
   dryRunOption,
   parallelDownloadsOption,
   configArgument
};
downloadCommand.SetHandler((isVerbose, isDryRun, parallelDownloads, configFile)
      => Console.WriteLine($"{isVerbose} ; {isDryRun} ; {parallelDownloads} ; {configFile}"),
   verboseOption, dryRunOption, parallelDownloadsOption, configArgument);

rootCommand.Add(downloadCommand);

await rootCommand.InvokeAsync(args);