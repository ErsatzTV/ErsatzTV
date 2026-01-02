using System.Collections.Concurrent;
using System.Diagnostics;

namespace ErsatzTV.Infrastructure;

public static class ScanProfiler
{
    private static readonly ConcurrentDictionary<string, ConcurrentBag<long>> Measurements = new();

    public static IDisposable Measure(string operationName)
    {
        return new TimerToken(operationName);
    }

    public static void Reset() => Measurements.Clear();

    public static void LogStatistics(Action<string> logAction)
    {
        if (Measurements.IsEmpty)
        {
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Scan Performance Summary:");
        sb.AppendLine(FormattableString.Invariant($"{"Operation",-25} | {"Count",-6} | {"Avg (ms)",-8} | {"Min",-6} | {"Max",-6} | {"P99",-6} | {"Total (s)",-8}"));
        sb.AppendLine(new string('-', 85));

        foreach (string key in Measurements.Keys.OrderBy(k => k))
        {
            var times = Measurements[key].ToList();
            if (times.Count == 0) continue;

            times.Sort();
            double avg = times.Average();
            long min = times.Min();
            long max = times.Max();
            long p99 = times[(int)(times.Count * 0.99)];
            double totalSec = times.Sum() / 1000.0;

            sb.AppendLine(FormattableString.Invariant($"{key,-25} | {times.Count,-6} | {avg,-8:F1} | {min,-6} | {max,-6} | {p99,-6} | {totalSec,-8:F2}"));
        }

        logAction(sb.ToString());
    }

    private readonly struct TimerToken(string name) : IDisposable
    {
        private readonly long _startTime = Stopwatch.GetTimestamp();

        public void Dispose()
        {
            long elapsedMs = (long)Stopwatch.GetElapsedTime(_startTime).TotalMilliseconds;
            ConcurrentBag<long> bag = Measurements.GetOrAdd(name, _ => []);
            bag.Add(elapsedMs);
        }
    }
}
