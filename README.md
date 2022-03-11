# Regen

Regen is a cli tool that regenerates every package in azure-sdk-for-net whose namespace starts with `Azure.ResourceManager`.

## Usage

You could either

1. Build the solution and run the executable.
1. Go to directory `Regen` and use `dotnet run` and necessary argument to run the tool.

### Argument and options

One required argument of this tool is the root directory path, you need to assign a path (absolute path will be the best) to run the tool:

```
dotnet run /path/to/azure-sdk-for-net
```

For other options, please see the help message of the tool.
