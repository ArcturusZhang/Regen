// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using regen;

const string DefaultProgressFilepath = "./progress.txt";
const int DefaultTimeoutInSeconds = 1800;

var rootDirArgument = new Argument<string>(
    name: "root",
    description: "The root directory of azure-sdk-for-net");

var processOption = new Option<string>(
    name: "--process",
    description: "The process file which stores the process of last execution.",
    getDefaultValue: () => DefaultProgressFilepath);

var timeoutOption = new Option<int>(
    name: "--timeout",
    description: "The timeout of one command in seconds",
    getDefaultValue: () => DefaultTimeoutInSeconds);

var skipOption = new Option<string[]?>(
    name: "--skip",
    description: "The namespace to be skipped in this round of regeneration")
{
    AllowMultipleArgumentsPerToken = true,
};

var rootCommand = new RootCommand("Cli tool to regenerate all the track 2 management packages in azure-sdk-for-net");
rootCommand.AddArgument(rootDirArgument);
rootCommand.AddOption(processOption);
rootCommand.AddOption(timeoutOption);
rootCommand.AddOption(skipOption);

// set default handler of argument and options
rootCommand.SetHandler(
    (string rootDir, string processFilepath, int timeout, string[]? skip) => new Regen(rootDir, processFilepath: processFilepath, timeout: timeout * 1000, skip: skip).Start(), rootDirArgument, processOption, timeoutOption, skipOption);

return await rootCommand.InvokeAsync(args);
