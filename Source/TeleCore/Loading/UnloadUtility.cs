using System;
using System.Collections.Generic;

namespace TeleCore.Loading;

public static class UnloadUtility
{
    internal static Action MemoryUnloadEvent;

    public static void RegisterUnloadAction(Action action)
    {
        MemoryUnloadEvent += action;
    }
}