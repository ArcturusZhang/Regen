using System.Diagnostics;
using System.Management.Automation;

namespace regen
{
    internal class Regen
    {
        private string _root;
        private int _threads;
        private string _processFilepath;
        private string? _lastSuccess;
        private bool _skipTest;
        private HashSet<string> _skip;
        private int _timeout;

        public Regen(string root, string processFilepath, int timeout, bool skipTest, string[]? skip = null, int threads = 1)
        {
            _root = root;
            _threads = threads;
            _processFilepath = processFilepath;
            _lastSuccess = GetProgress(_processFilepath);
            _skipTest = skipTest;
            _skip = new HashSet<string>(skip ?? Enumerable.Empty<string>());
            _timeout = timeout;
        }

        public void Start()
        {
            var watch = Stopwatch.StartNew();
            bool foundLastStartingPoint = _lastSuccess is null;
            Execute(_root, _skip, ref _lastSuccess, foundLastStartingPoint, _timeout, _skipTest);
            Console.WriteLine($"Finished everything in {watch.Elapsed.TotalMinutes}");
            SaveProgress();
        }

        public static void Execute(string root, HashSet<string> skip, ref string? lastSuccess, bool foundLastStartingPoint, int timeout, bool skipTest)
        {
            foreach (var serviceDir in Directory.GetDirectories(Path.Combine(root, "sdk")))
            {
                foreach (var solutionDir in Directory.GetDirectories(serviceDir))
                {
                    var dirName = Path.GetFileName(solutionDir);
                    if (skip.Contains(dirName))
                    {
                        WriteLine(ConsoleColor.White, $"------------------Ignoring {dirName}------------------");
                        continue;
                    }

                    if (!foundLastStartingPoint)
                    {
                        if (dirName is not null && dirName.StartsWith("Azure.ResourceManager"))
                            WriteLine(ConsoleColor.White, $"------------------Skipping {dirName}------------------");
                        if (solutionDir == lastSuccess)
                        {
                            foundLastStartingPoint = true;
                        }
                        continue;
                    }

                    if (dirName is null || !dirName.StartsWith("Azure.ResourceManager"))
                        continue;

                    if (RegenDirectory(solutionDir, dirName, timeout, skipTest))
                    {
                        lastSuccess = solutionDir;
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        public static bool RegenDirectory(string solutionDir, string dirName, int timeout, bool skipTest)
        {
            return ExecuteDotnet(solutionDir, "dotnet", dirName, "restore", timeout) &&
                ExecuteDotnet(solutionDir, "dotnet", dirName, "build /t:GenerateCode", timeout) &&
                ExecuteDotnet(solutionDir, "dotnet", dirName, "test", timeout, skipTest);
            //if (!ExecutePs(solutionDir, Path.Combine(root, "eng", "scripts", "Export-API.ps1"), dirName, Path.GetFileName(serviceDir))) return;
        }

        private static bool ExecutePs(string solutionDir, string script, string dirName, string args)
        {
            var command = Path.GetFileName(script);
            var watch = Stopwatch.StartNew();
            try
            {
                WriteLine(ConsoleColor.White, $"------------------Starting {command} on {dirName}------------------");
                var ps = PowerShell.Create(RunspaceMode.NewRunspace).AddScript(script).AddArgument(args).Invoke();
                watch.Stop();
                WriteLine(ConsoleColor.White, $"------------------Finished({watch.Elapsed.TotalSeconds}) {command} on {dirName}------------------");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                watch.Stop();
                WriteLine(ConsoleColor.Red, $"------------------Finished({watch.Elapsed.TotalSeconds}) {command} on {dirName}------------------");
                return false;
            }
        }

        private void SaveProgress()
        {
            if (string.IsNullOrEmpty(_lastSuccess)) return;

            if (File.Exists(_processFilepath)) File.Delete(_processFilepath);
            File.WriteAllText(_processFilepath, _lastSuccess);
        }

        private static string? GetProgress(string processFilepath)
        {
            try
            {
                return File.Exists(processFilepath) ? File.ReadAllText(processFilepath) : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool ExecuteDotnet(string solutionDir, string program, string dirName, string command, int timeout, bool skip = false)
        {
            if (skip)
            {
                WriteLine(ConsoleColor.White, $"------------------Skipping {command} on {dirName}------------------");
                return true;
            }
            var watch = Stopwatch.StartNew();
            string message;
            bool success = true;
            WriteLine(ConsoleColor.White, $"------------------Starting {command} on {dirName}------------------");
            var exitColor = ConsoleColor.White;
            using (Process process = new Process())
            {
                process.StartInfo.WorkingDirectory = solutionDir;
                process.StartInfo.Arguments = command;
                process.StartInfo.FileName = program;
                process.StartInfo.UseShellExecute = false;
                //process.StartInfo.RedirectStandardOutput = true;
                //process.StartInfo.RedirectStandardError = true;
                process.Start();
                process.WaitForExit(timeout);
                if (!process.HasExited)
                {
                    message = "KILLING";
                    process.Kill();
                    success = false;
                    exitColor = ConsoleColor.Red;
                }
                else
                {
                    if (process.ExitCode != 0)
                    {
                        success = false;
                        exitColor = ConsoleColor.Red;
                        //Console.WriteLine(process.StandardError.ToString());
                    }
                    message = process.ExitCode == 0 ? "Finished" : "Failed";
                }
            }
            watch.Stop();
            WriteLine(exitColor, $"------------------{message}({watch.Elapsed.TotalSeconds}) {command} on {dirName}------------------");
            return success;
        }

        private static void WriteLine(ConsoleColor color, string message)
        {
            var current = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = current;
        }
    }
}
