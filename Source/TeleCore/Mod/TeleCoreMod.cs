using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class TeleCoreMod : Mod
    {
        //Static Data
        private static TeleCoreMod modInt;
        private static Harmony teleCore;

        public static TeleCoreMod Mod => modInt;
        public static Harmony TeleCore
        {
            get
            {
                Harmony.DEBUG = true;
                return teleCore ??= new Harmony("telefonmast.telecore");
            }
        }

        public static TeleCoreSettings Settings => (TeleCoreSettings)modInt.modSettings;

        public TeleCoreMod(ModContentPack content) : base(content)
        {
            modInt = this;
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version.ToString();
            
            //
            TLog.Message($"[{version}] - Init", Color.cyan);
            modSettings = GetSettings<TeleCoreSettings>();

            //
            TeleCore.PatchAll(assembly);

        }

        public override string SettingsCategory()
        {
            return "TeleCore";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            TWidgets.DoTinyLabel(inRect, "Hi :)");
        }
    }
}
