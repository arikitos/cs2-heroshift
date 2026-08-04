using src.utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using static src.HeroShift;

namespace src.player
{
    /*
     * PerfLog - timing instrumentation for slow hooks, gated on
     * ConfigurationStore.Settings.General.PerfMode. Writes to <plugin>/logs/perf_<session>.txt.
     *
     * Usage from a hero or hook is always the same two-step pattern:
     *
     *     long t = PerfLog.Start();          // returns 0 when PerfMode is off
     *     ... work ...
     *     PerfLog.End("MySkill.OnTick", t);  // or Sample(...) on a per-tick path
     *
     * Start() returns 0 when disabled and both End() and Sample() bail out on a 0
     * timestamp, so instrumented code costs one comparison when PerfMode is off and
     * needs no #if or extra branching at the call site.
     *
     * Which of the two reporting methods to use matters:
     *   End()    - one-shot paths (a skill activation, a round transition). Logs a
     *              single line, but only when the measured time reaches thresholdMs
     *              (default 1ms), so fast calls stay silent.
     *   Sample() - per-tick paths. Logging every tick would produce ~64 lines per
     *              second per label, so it accumulates into a per-label Aggregate and
     *              emits one avg/max/samples summary every reportSeconds (default 5),
     *              and only if the window's max reached maxThresholdMs (default
     *              0.5ms). Quiet windows produce nothing at all.
     *
     * Timing uses Stopwatch.GetTimestamp ticks converted with Stopwatch.Frequency,
     * not DateTime, so it is monotonic and unaffected by clock changes. The
     * aggregate window boundary itself is checked with DateTime.
     *
     * Note the file is created on the first write rather than at load, and the very
     * first Start() emits a "PerfMode enabled" header line precisely so an operator
     * can tell PerfMode is active even if nothing has been slow yet. Everything is
     * serialised on _writeLock / the per-Aggregate lock because tick hooks and event
     * handlers can both call in.
     */
    public static class PerfLog
    {
        private static readonly string logsFolder = Path.Combine(Instance.ModuleDirectory, "logs");
        private static readonly string sessionId = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        private static StreamWriter? _writer;
        private static readonly object _writeLock = new();

        public static bool Enabled => ConfigurationStore.Settings.General.PerfMode;

        private static bool _headerWritten;

        // Begins a measurement. Returns 0 when PerfMode is off, which is the sentinel
        // End()/Sample() use to skip their work entirely.
        public static long Start()
        {
            if (!Enabled) return 0;

            // First write creates the file so an active PerfMode is immediately visible.
            if (!_headerWritten)
            {
                _headerWritten = true;
                Write($"PerfMode enabled (plugin v{Instance.ModuleVersion})");
            }

            return Stopwatch.GetTimestamp();
        }

        // Free-form note in the perf log, for context around the timing lines.
        public static void Info(string message)
        {
            if (!Enabled) return;
            Write(message);
        }

        // One-shot measurement: logs "label took X.XXms" when the elapsed time reaches the threshold.
        public static void End(string label, long startTimestamp, double thresholdMs = 1.0)
        {
            if (startTimestamp == 0 || !Enabled) return;

            // Stopwatch ticks are not milliseconds; Frequency is ticks per second.
            double ms = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
            if (ms < thresholdMs) return;

            Write($"{label} took {ms:F2}ms");
        }

        // Rolling window of samples for one label; reset each time a summary is emitted.
        private sealed class Aggregate
        {
            public double TotalMs;
            public double MaxMs;
            public int Count;
            public DateTime WindowStart = DateTime.Now;
        }

        private static readonly ConcurrentDictionary<string, Aggregate> _aggregates = new();

        // Per-tick measurement: accumulates and logs an avg/max summary every few seconds,
        // so tick paths do not produce one log line per tick.
        public static void Sample(string label, long startTimestamp, double reportSeconds = 5.0, double maxThresholdMs = 0.5)
        {
            if (startTimestamp == 0 || !Enabled) return;

            double ms = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
            var agg = _aggregates.GetOrAdd(label, _ => new Aggregate());
            lock (agg)
            {
                agg.TotalMs += ms;
                agg.Count++;
                if (ms > agg.MaxMs) agg.MaxMs = ms;

                if ((DateTime.Now - agg.WindowStart).TotalSeconds < reportSeconds) return;

                if (agg.MaxMs >= maxThresholdMs)
                    Write($"{label} avg={agg.TotalMs / agg.Count:F2}ms max={agg.MaxMs:F2}ms samples={agg.Count}");

                agg.TotalMs = 0;
                agg.MaxMs = 0;
                agg.Count = 0;
                agg.WindowStart = DateTime.Now;
            }
        }

        // Single writer for the perf file. Opens it lazily on first use and swallows all
        // IO errors, since this runs inside game hooks where throwing is not acceptable.
        private static void Write(string message)
        {
            lock (_writeLock)
            {
                try
                {
                    if (_writer == null)
                    {
                        Directory.CreateDirectory(logsFolder);
                        _writer = new StreamWriter(Path.Combine(logsFolder, $"perf_{sessionId}.txt"), append: true, System.Text.Encoding.UTF8) { AutoFlush = true };
                    }
                    _writer.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [PERF] {message}");
                }
                catch
                {
                }
            }
        }
    }
}
