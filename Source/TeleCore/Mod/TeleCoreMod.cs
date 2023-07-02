using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace TeleCore;

public class TeleCoreMod : Mod
{
    //Static Data
    private static Harmony teleCore;

    public TeleCoreMod(ModContentPack content) : base(content)
    {
        Mod = this;
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version.ToString();

        //
        TLog.Message($"[{version}] - Init", Color.cyan);
        modSettings = GetSettings<TeleCoreSettings>();

        //
        TeleCore.PatchAll(assembly);
    }

    public static TeleCoreMod Mod { get; private set; }

    public static Harmony TeleCore
    {
        get
        {
            Harmony.DEBUG = true;
            return teleCore ??= new Harmony("telefonmast.telecore");
        }
    }

    public static TeleCoreSettings Settings => (TeleCoreSettings) Mod.modSettings;

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