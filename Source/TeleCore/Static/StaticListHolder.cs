using System.Collections.Generic;

namespace TeleCore.Static;

public static class StaticListHolder<T>
{
    internal static Dictionary<string, List<T>> NamedWorkerLists = new Dictionary<string, List<T>>();

    public static List<T> RequestList(string uniqueID)
    {
        if (!NamedWorkerLists.TryGetValue(uniqueID, out var list))
        {
            list = new List<T>();
            NamedWorkerLists.Add(uniqueID, list);
        }
        return list;
    }
    
}