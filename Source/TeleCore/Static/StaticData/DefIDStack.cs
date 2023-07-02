using System.Collections.Generic;
using Verse;

namespace TeleCore;

public static class DefIDStack
{
    internal static ushort _MasterID;
    internal static Dictionary<Def, ushort> _defToID;
    internal static Dictionary<ushort, Def> _idToDef;

    static DefIDStack()
    {
        _defToID = new Dictionary<Def, ushort>();
        _idToDef = new Dictionary<ushort, Def>();
    }

    public static ushort ToID(Def def)
    {
        if (_defToID.TryGetValue(def, out var id)) return id;
        TLog.Warning($"Cannot find id for ({def.GetType()}){def}. Make sure to call base.PostLoad().");
        return def.index;
    }

    public static TDef ToDef<TDef>(ushort id)
        where TDef : Def
    {
        if (_idToDef.TryGetValue(id, out var def))
        {
            if (def is TDef casted)
                return casted;
            TLog.Warning($"Cannot cast {def} to {typeof(TDef)}");
            return null;
        }

        TLog.Warning($"Cannot find Def for {id} of type {typeof(TDef)}. Make sure PostLoad calls base.PostLoad.");
        return null;
    }

    public static void RegisterNew<TDef>(TDef def) where TDef : Def
    {
        if (_defToID.ContainsKey(def))
        {
            TLog.Warning($"{def} is already registered.");
            return;
        }

        _defToID.Add(def, _MasterID);
        _idToDef.Add(_MasterID, def);
        _MasterID++;
    }
}