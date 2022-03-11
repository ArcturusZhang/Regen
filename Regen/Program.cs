// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using regen;

var rootDirArgument = new Argument<string>(
    name: "root",
    description: "The root directory of azure-sdk-for-net");

var processOption = new Option<string?>(
            name: "--process",
            description: "The process file which stores the process of last execution.");

var rootCommand = new RootCommand("Cli tool to regenerate all the track 2 management packages in azure-sdk-for-net");
rootCommand.AddArgument(rootDirArgument);
rootCommand.AddOption(processOption);

// set default handler of argument and options
rootCommand.SetHandler(
    (string rootDir, string? processFilepath) => new Regen(rootDir, processFilepath: processFilepath).Start(), rootDirArgument, processOption);

return await rootCommand.InvokeAsync(args);
