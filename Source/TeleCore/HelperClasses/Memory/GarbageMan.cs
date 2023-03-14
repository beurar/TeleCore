using System;

namespace TeleCore.Memory;

/// <summary>
/// Calls the GC after being used.
/// Use in any method with a using statement to ensure garbage collection when finished.
/// </summary>
public class GarbageMan : IDisposable
{
    public void Dispose()
    {
        GC.Collect();
    }
}