using UnityEngine;
using Verse;

namespace TeleCore;

[StaticConstructorOnStartup]
public static class TFind
{
    internal static readonly GameObject TeleRootHolder;
    internal static readonly TeleRoot MainRoot;

    static TFind()
    {
        TeleRootHolder = new GameObject("TeleCoreHolder");
        Object.DontDestroyOnLoad(TeleRootHolder);
        TeleRootHolder.AddComponent<TeleRoot>();

        MainRoot = TeleRootHolder.GetComponent<TeleRoot>();
        TLog.Message("TFind Ready!", TColor.Green);
    }

    public static TeleRoot TeleRoot => MainRoot;
    public static TeleTickManager TickManager => TeleRoot.TickManager;
    public static DiscoveryTable Discoveries => StaticData.TeleWorldComp(Find.World.GetUniqueLoadID()).discoveries;
    
    public static GameComponent_CameraPanAndLock CameraPanNLock()
    {
        return Current.Game.GetComponent<GameComponent_CameraPanAndLock>();
    }

}