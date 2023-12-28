using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TeleCore.Loading;
using TeleCore.Primitive;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;
using WidgetRow = TeleCore.Rendering.WidgetRow;

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
        
        //MethodInfo target = typeof(ToilFailConditions).GetNestedTypes(AccessTools.all).SelectMany(AccessTools.GetDeclaredMethods).First(mi => mi.Name.Contains("FailOnChildLearningConditions"));
        // TeleCore.Patch(
        //     original: AccessTools.Method(target.DeclaringType.MakeGenericType(new Type[] { typeof(IJobEndable) }), target.Name),
        //     transpiler: new HarmonyMethod(typeof(HarmonyPatcher), nameof(ReplaceDevelopmentalStageGeneralTranspiler))
        // );
        
        // var originalMethod = typeof(GenAttribute).GetMethod(nameof(GenAttribute.TryGetAttribute),new[] { typeof(MemberInfo), typeof(object) })
        //     ?.MakeGenericMethod(typeof(ReplacePatches.GenAttribute_Patch)); //replace YourType with the type you want to pass
        // TLog.Debug($"Found method: {originalMethod}");
        // var prefixMethodInfo = typeof(ReplacePatches.GenAttribute_Patch).GetMethod(nameof(ReplacePatches.GenAttribute_Patch.Prefix));
        // var prefixMethod = new HarmonyMethod(prefixMethodInfo);
        // TeleCore.Patch(originalMethod, prefix: prefixMethod);
        
        //
        TLog.Message($"[{version}] - Init", Color.cyan);
        modSettings = GetSettings<TeleCoreSettings>();

        //
        TeleCore.PatchAll(assembly);
        
        TeleParseHelper.Init();
    }

    public static TeleCoreMod Mod { get; private set; }

    public static Harmony TeleCore
    {
        get
        {
            Harmony.DEBUG = false;
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
        Listing_Standard ls = new Listing_Standard(GameFont.Small);
        ls.Begin(inRect);
        ls.Label("Some Settings");
        ls.CheckboxLabeled("Show Tele Tools in main menu", ref Settings.showToolsInMainMenu);
        ls.NewColumn();
        ls.End();
    }
}