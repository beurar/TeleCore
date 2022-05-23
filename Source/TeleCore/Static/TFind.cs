using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }

        public static TeleRoot TeleRoot => mainRoot;
        public static TeleTickManager TickManager => TeleRoot.TickManager;
    }
}
