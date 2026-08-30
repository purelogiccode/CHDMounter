using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

internal static class SequentialTestRunner
{
    internal const int DefaultMaxFilesPerCollection = 10;
    internal const int DefaultMaxDegreeOfParallelism = 3;

    internal static List<string> CollectPaths(params string[] directories)
    {
        return CollectPaths(DefaultMaxFilesPerCollection, directories);
    }

    internal static List<string> CollectPaths(IEnumerable<string> directories)
    {
        return CollectPaths(DefaultMaxFilesPerCollection, directories);
    }

    internal static List<string> CollectPaths(int maxFiles, params string[] directories)
    {
        return CollectPaths(maxFiles, (IEnumerable<string>)directories);
    }

    internal static List<string> CollectPaths(int maxFiles, IEnumerable<string> directories)
    {
        var paths = new List<string>();
        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir)) continue;

            var dirPaths = Directory.EnumerateFiles(dir, "*.chd", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Take(maxFiles);
            paths.AddRange(dirPaths);
        }

        return paths;
    }

    internal static void Run(ITestOutputHelper output, string testName, List<string> chdPaths,
        Func<string, ITestOutputHelper, bool> testFunc)
    {
        var failures = new ConcurrentBag<(string path, string error)>();
        var outputLock = new Lock();
        var syncOutput = new SynchronizedTestOutputHelper(output, outputLock);
        int passed = 0, skipped = 0;
        var sw = Stopwatch.StartNew();

        Parallel.ForEach(chdPaths, new ParallelOptions { MaxDegreeOfParallelism = DefaultMaxDegreeOfParallelism },
            chdPath =>
            {
                if (!File.Exists(chdPath))
                {
                    syncOutput.WriteLine($"SKIP: {chdPath} not found");
                    Interlocked.Increment(ref skipped);
                    return;
                }

                var fileName = Path.GetFileName(chdPath);
                try
                {
                    syncOutput.WriteLine($"--- {fileName} ---");
                    if (testFunc(chdPath, syncOutput))
                        Interlocked.Increment(ref passed);
                    else
                        failures.Add((chdPath, $"{testName} returned false for {fileName}"));
                }
                catch (Exception ex)
                {
                    failures.Add((chdPath, $"{ex.GetType().Name}: {ex.Message}"));
                    syncOutput.WriteLine($"  FAIL: {ex.GetType().Name}: {ex.Message}");
                }
            });

        sw.Stop();
        output.WriteLine(
            $"{testName}: {passed} passed, {skipped} skipped, {failures.Count} failed in {sw.Elapsed.TotalSeconds:F1}s");

        Assert.True(failures.IsEmpty,
            $"{failures.Count} failures in {testName}:\n" +
            string.Join('\n', failures.Select(static f => $"  {Path.GetFileName(f.path)} - {f.error}")));
    }

    private sealed class SynchronizedTestOutputHelper : ITestOutputHelper
    {
        private readonly ITestOutputHelper _inner;
        private readonly Lock _lock;

        public SynchronizedTestOutputHelper(ITestOutputHelper inner, Lock @lock)
        {
            _inner = inner;
            _lock = @lock;
        }

        public void WriteLine(string message)
        {
            lock (_lock)
            {
                _inner.WriteLine(message);
            }
        }

        public void WriteLine(string format, params object[] args)
        {
            lock (_lock)
            {
                _inner.WriteLine(format, args);
            }
        }
    }
}