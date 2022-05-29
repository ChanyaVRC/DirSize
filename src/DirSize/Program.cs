using BuildSoft.Command.DirSize;
using BuildSoft.Command.DirSize.Logging;
using BuildSoft.Command.DirSize.Table;
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
        WriteResult(results, Console.Out, logger);

        stopwatch.Stop();

        logger.Log();
        logger.Log($"----------------------------------------------------");
        logger.Log();
        logger.Log($"Evaluted time : {evalutedTime}");
        logger.Log($"Output time   : {stopwatch.Elapsed - evalutedTime}");
        logger.Log($"Running time  : {stopwatch.Elapsed}");
    }

    static async ValueTask<DirectorieContent> GetDirectorySizeAsync(
        DirectoryInfo directory,
        Dictionary<DirectoryInfo, DirectorieContent>[] results,
        CancellationToken token)
    {
        FileInfo[] files;
        try
        {
            files = directory.GetFiles();
        }
        catch (UnauthorizedAccessException) { return new(); }
        catch (DirectoryNotFoundException) { return new(); }

        long size = 0;
        for (int i = 0; i < files.Length; i++)
        {
            size += files[i].Length;
        }
        int fileCount = files.Length;
        int directoryCount = 0;

        if (_parameters.IsNeedToAnalyzeRecursively)
        {
            await Parallel.ForEachAsync(directory.EnumerateDirectories(), token, async (directory, token) =>
            {
                var subDirectoryContent = await GetDirectorySizeAsync(directory, results, token);
                if (_parameters.IsIncludingSubDirectoriesSize)
                {
                    Interlocked.Add(ref size, subDirectoryContent.Size);
                    Interlocked.Add(ref fileCount, subDirectoryContent.FileCount);
                    Interlocked.Add(ref directoryCount, subDirectoryContent.DirectoryCount + 1);
                }
            });
        }

        long directoryHash = Unsafe.As<DirectoryInfo, IntPtr>(ref directory).ToInt64();
        var list = results[directoryHash % results.Length];
        DirectorieContent content = new(size, fileCount, directoryCount);
        lock (list)
        {
            list.Add(directory, content);
        }
        return content;
    }


    static DirectorieContent GetDirectorySize(
        DirectoryInfo directory,
        Dictionary<DirectoryInfo, DirectorieContent> results)
    {
        FileInfo[] files;
        try
        {
            files = directory.GetFiles();
        }
        catch (UnauthorizedAccessException) { return new(); }
        catch (DirectoryNotFoundException) { return new(); }

        long size = 0;
        for (int i = 0; i < files.Length; i++)
        {
            size += files[i].Length;
        }
        int fileCount = files.Length;
        int directoryCount = 0;

        if (_parameters.IsNeedToAnalyzeRecursively)
        {
            foreach (var d in directory.EnumerateDirectories())
            {
                var subDirectoryContent = GetDirectorySize(d, results);
                if (_parameters.IsIncludingSubDirectoriesSize)
                {
                    size += subDirectoryContent.Size;
                    fileCount += subDirectoryContent.FileCount;
                    directoryCount += subDirectoryContent.DirectoryCount + 1;
                }
            }
        }

        DirectorieContent content = new(size, fileCount, directoryCount);
        results.Add(directory, content);
        return content;
    }

    static async ValueTask<EvaluateResults> EvaluateAsync()
    {
        DirectoryInfo root = new(_parameters.RootDirectory);

        IEnumerable<KeyValuePair<DirectoryInfo, DirectorieContent>> results;
        long sumSize = 0;

        if (_parameters.IsSingleTask)
        {
            Dictionary<DirectoryInfo, DirectorieContent>? singleTaskResults = new(1024);
            foreach (var directory in root.EnumerateDirectories())
            {
                var content = GetDirectorySize(directory, singleTaskResults);
                sumSize += content.Size;
            }

            results = singleTaskResults;
        }
        else
        {
            var multiTaskResults = new Dictionary<DirectoryInfo, DirectorieContent>[3];
            for (int i = 0; i < multiTaskResults.Length; i++)
            {
                multiTaskResults[i] = new(1024);
            }
            await Parallel.ForEachAsync(root.EnumerateDirectories(), async (directory, token) =>
            {
                var content = await GetDirectorySizeAsync(directory, multiTaskResults, token);
                Interlocked.Add(ref sumSize, content.Size);
            });

            results = multiTaskResults!.SelectMany(v => v);
        }

        if (!_parameters.IsRecursively)
        {
            results = root.EnumerateDirectories()
                .Join(results, outer => outer.FullName, inner => inner.Key.FullName, (_, inner) => inner);
        }

        return new(results, sumSize);
    }

    static void WriteResult(EvaluateResults results, TextWriter writer, ILogger logger)
    {
        int count = 0;

        var contents = results.DirectoryContents.OrderBy(v => v.Key.FullName);

        switch (_parameters.EmitFormat)
        {
            case EmitFormat.None:
            case EmitFormat.FixedLength:
            case EmitFormat.Csv:
            case EmitFormat.Tsv:
                TableEmitter<KeyValuePair<DirectoryInfo, DirectorieContent>> emitter = new();
                emitter.RegisterColumn(new("Directory Path", ColumnType.String, v => v.Key.FullName));
                emitter.RegisterColumn(new("Size", ColumnType.Int64, v => v.Value.Size.ToString()));
                emitter.RegisterColumn(new("File Count", ColumnType.Int32, v => v.Value.FileCount.ToString()));
                emitter.RegisterColumn(new("Directory Count", ColumnType.Int32, v => v.Value.DirectoryCount.ToString()));

                foreach (var rows in emitter.Emit(contents, _parameters.EmitFormat, true))
                {
                    writer.WriteLine(rows);
                }

                break;
            default:
                Debug.Fail("Undefined format requested.");
                break;
        }

        logger.Log();
        logger.Log($"Target directory size : {results.SumSize}");
        logger.Log($"Directories count : {count}");
    }

    private record struct DirectorieContent(long Size, int FileCount, int DirectoryCount);
    private record struct EvaluateResults(IEnumerable<KeyValuePair<DirectoryInfo, DirectorieContent>> DirectoryContents, long SumSize);

}