using System;

namespace TeleCore.Data.Events;

public static class TeleEventHandler
{
    public static event EntityTickedEvent EntityTicked;

    internal static void OnEntityTicked()
    {
        try
        {
            EntityTicked?.Invoke();
        }
        catch (Exception ex)
        {
            TLog.Error($"Error trying to tick entities:\n{ex.Message}");
        }
    }
}