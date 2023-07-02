using System.Collections.Generic;

namespace TeleCore.Static;

public static class StaticListHolder<T>
{
    internal static Dictionary<string, List<T>> NamedWorkerLists = new();
    internal static Dictionary<string, HashSet<T>> NamedWorkerSets = new();


    public static List<T> RequestList(string uniqueID)
    {
        if (!NamedWorkerLists.TryGetValue(uniqueID, out var list))
        {
            list = new List<T>();
            NamedWorkerLists.Add(uniqueID, list);
        }

        return list;
    }

    public static HashSet<T> RequestSet(string uniqueID)
    {
        if (!NamedWorkerSets.TryGetValue(uniqueID, out var list))
        {
            list = new HashSet<T>();
            NamedWorkerSets.Add(uniqueID, list);
        }

        return list;
    }
}