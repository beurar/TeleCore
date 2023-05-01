using System;
using UnityEngine;
using Verse;

namespace TeleCore
{
    [StaticConstructorOnStartup]
    public static class TFind
    {
        internal static GameObject teleRootHolder;
        internal static TeleRoot mainRoot;

        static TFind()
        {
            teleRootHolder = new GameObject("TeleCoreHolder");
            UnityEngine.Object.DontDestroyOnLoad(teleRootHolder);
            teleRootHolder.AddComponent<TeleRoot>();

            mainRoot = teleRootHolder.GetComponent<TeleRoot>();
            TLog.Message("TFind Ready!", TColor.Green);
        }

        public static TeleRoot TeleRoot => mainRoot;
        public static TeleTickManager TickManager => TeleRoot.TickManager;
        public static DiscoveryTable Discoveries => StaticData.TeleCoreWorldComp._discoveries;
    }
}
