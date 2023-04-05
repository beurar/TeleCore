using System.Diagnostics;

namespace TeleCore.Static.Utilities;

public static class TProfiler
{
    private static bool _Disabled;
    
    private static Stopwatch Watch { get; }
    private static string CurScope { get; set; }

    public static bool IsActive => !_Disabled && Watch.IsRunning;
    
    static TProfiler()
    {
        Watch = new Stopwatch();
    }

    public static void Begin(string label = null)
    {
        //TODO: Custom log file
        return;
        
        //
        if (_Disabled)
        {
            return;
        }
        CurScope = label ?? null;
        Watch.Reset();
        Watch.Start();
    }

    public static void Check(string tag = null)
    {
        //TODO: Custom log file
        return;
        if (!Watch.IsRunning)
        {
            TLog.Warning("Cant check profiling while inactive.");
            return;
        }
        
        TLog.Debug($"[{tag}]: {Watch.Elapsed.TotalMilliseconds}ms");
        
    }
    
    public static void End(string message = null)
    {
        //TODO: Custom log file
        return;
        if (!Watch.IsRunning)
        {
            return;
        }
        Watch.Stop();
        TLog.Debug($"{(CurScope != null ? $"[{CurScope}]: " : null)}Profiling Result: {Watch.Elapsed.TotalMilliseconds}ms");
    }

    internal static void Disable()
    {
        _Disabled = true;
    }

    internal static void Enable()
    {
        _Disabled = false;
    }
}