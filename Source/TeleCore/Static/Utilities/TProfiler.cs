using System.Diagnostics;

namespace TeleCore.Static.Utilities;

public static class TProfiler
{
    private static Stopwatch Watch { get; }

    private static string CurScope { get; set; }
    
    static TProfiler()
    {
        Watch = new Stopwatch();
    }

    public static void Begin(string label = null)
    {
        CurScope = label ?? null;
        Watch.Reset();
        Watch.Start();
    }

    public static void Check()
    {
        if (!Watch.IsRunning)
        {
            TLog.Warning("Cant check profiling while not running.");
            return;
        }
        
        TLog.Debug($"ProfilingCheck: {Watch.Elapsed}");
        
    }
    
    public static void End(string message = null)
    {
        Watch.Stop();
        TLog.Debug($"{(CurScope != null ? $"[{CurScope}]: " : null)}Profiling Result: {Watch.Elapsed}");
    }
}