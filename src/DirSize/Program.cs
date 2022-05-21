using BuildSoft.Command.DirSize;
using BuildSoft.Command.DirSize.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

internal class Program
{
    static readonly DirSizeParameters _parameters;

    static Program()
    {
        _parameters = new(Environment.GetCommandLineArgs().Skip(1));
    }

    private static async Task Main()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        ILogger logger = _parameters.IsSilent ? new DummyLogger() : new ConsoleLogger();

        if (!Directory.Exists(_parameters.RootDirectory))
        {
            if (File.Exists(_parameters.RootDirectory))
            {
                logger.Log($"\"{_parameters.RootDirectory}\" is file. Please enter the directory.");
            }
            else
            {
                logger.Log($"\"{_parameters.RootDirectory}\" is not exist.");
            }
            return;
        }

        var results = await EvaluateAsync();
        var evalutedTime = stopwatch.Elapsed;
        WriteResult(results);

        stopwatch.Stop();

        logger.Log();
        logger.Log($"Evaluted time : {evalutedTime}");
        logger.Log($"Output time   : {stopwatch.Elapsed - evalutedTime}");
        logger.Log($"Running time  : {stopwatch.Elapsed}");

        return;
    }

    static async ValueTask<long> GetDirectorySizeAsync(DirectoryInfo directory, Dictionary<DirectoryInfo, long>[] results, CancellationToken token)
    {
        FileInfo[] files;
        try
        {
            files = directory.GetFiles();
        }
        catch (UnauthorizedAccessException) { return 0; }
        catch (DirectoryNotFoundException) { return 0; }

        long size = 0;
        for (int i = 0; i < files.Length; i++)
        {
            size += files[i].Length;
        }
        await Parallel.ForEachAsync(directory.EnumerateDirectories(), token, async (directory, token) =>
        {
            Interlocked.Add(ref size, await GetDirectorySizeAsync(directory, results, token));
        });

        long directoryHash = Unsafe.As<DirectoryInfo, IntPtr>(ref directory).ToInt64();
        var list = results[directoryHash % results.Length];
        lock (list)
        {
            list.Add(directory, size);
        }
        return size;
    }


    static long GetDirectorySize(DirectoryInfo directory, Dictionary<DirectoryInfo, long> results)
    {
        FileInfo[] files;
        try
        {
            files = directory.GetFiles();
        }
        catch (UnauthorizedAccessException) { return 0; }
        catch (DirectoryNotFoundException) { return 0; }

        long size = 0;
        for (int i = 0; i < files.Length; i++)
        {
            size += files[i].Length;
        }
        foreach (var d in directory.EnumerateDirectories())
        {
            Interlocked.Add(ref size, GetDirectorySize(d, results));
        }

        results.Add(directory, size);
        return size;
    }

    static async ValueTask<IEnumerable<KeyValuePair<DirectoryInfo, long>>> EvaluateAsync()
    {
        DirectoryInfo root = new(_parameters.RootDirectory);

        Dictionary<DirectoryInfo, long>[]? multiTaskResults = null;
        Dictionary<DirectoryInfo, long>? singleTaskResults = null;
        if (_parameters.IsSingleTask)
        {
            singleTaskResults = new(1024);
            foreach (var directory in root.EnumerateDirectories())
            {
                GetDirectorySize(directory, singleTaskResults);
            }
        }
        else
        {
            multiTaskResults = new Dictionary<DirectoryInfo, long>[3];
            for (int i = 0; i < multiTaskResults.Length; i++)
            {
                multiTaskResults[i] = new(1024);
            }
            await Parallel.ForEachAsync(root.EnumerateDirectories(),
                async (directory, token) => await GetDirectorySizeAsync(directory, multiTaskResults, token));
        }

        var results = _parameters.IsSingleTask ? singleTaskResults! : multiTaskResults!.SelectMany(v => v);
        if (!_parameters.IsRecursively)
        {
            results = root.EnumerateDirectories()
                .Join(results, outer => outer.FullName, inner => inner.Key.FullName, (_, inner) => inner);
        }

        return results;
    }

    static void WriteResult(IEnumerable<KeyValuePair<DirectoryInfo, long>> results)
    {
        foreach (var (di, size) in results.OrderBy(v => v.Key.FullName))
        {
            if (_parameters.IsOutputAsCsv)
            {
                Console.WriteLine($"\"{di.FullName}\",{size}");
            }
            else
            {
                Console.WriteLine($"{di.FullName}\t{size}");
            }
        }
    }
}