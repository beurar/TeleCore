using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TeleCore
{
    public static class TFind
    {
        internal static GameObject teleRootHolder;
        private static TeleRoot rootInt;

        static TFind()
        {
            teleRootHolder = new GameObject("TeleCoreHolder");
            UnityEngine.Object.DontDestroyOnLoad(teleRootHolder);
            teleRootHolder.AddComponent<TeleRoot>();
        }

        public static TeleRoot TRoot
        {
            get => rootInt;
            set => rootInt = value;
        }

        public static TeleTickManager TickManager
        {
            get => TRoot.TickManager;
        }
    }
}
