using UnityEngine;
using Verse;

namespace TeleCore
{
    internal static class TLog
    {
        public static void Error(string msg, string tag = null)
        {
            Log.Error($"{"[TELE][ERROR]".Colorize(TColor.NiceBlue)} {msg}");
        }

        public static void ErrorOnce(string msg, int id)
        {
            Log.ErrorOnce($"{"[TELE]".Colorize(TColor.NiceBlue)} {msg}", id);
        }

        public static void Warning(string msg)
        {
            Log.Warning($"{"[TELE]".Colorize(TColor.NiceBlue)} {msg}");
        }

        public static void Message(string msg, Color color)
        {
            Log.Message($"{"[TELE]".Colorize(color)} {msg}");
        }

        public static void Message(string msg)
        {
            Log.Message($"{"[TELE]".Colorize(TColor.NiceBlue)} {msg}");
        }

        public static void Debug(string msg, bool flag = true)
        {
            if (flag)
            {
                Log.Message($"{"[TELE-Debug]".Colorize(TColor.Green)} {msg}");
            }
        }

        public static void DebugOnce(string msg, int hash)
        {
            object obj = Log.logLock;
            lock (obj)
            {
                if (!Log.ReachedMaxMessagesLimit)
                {
                    if (!Log.usedKeys.Contains(hash))
                    {
                        Log.usedKeys.Add(hash);
                        Debug(msg);
                    }
                }
            }
        }
    }
}
