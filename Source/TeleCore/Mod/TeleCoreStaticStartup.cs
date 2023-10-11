using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Multiplayer.API;
using Verse;

namespace TeleCore;

[StaticConstructorOnStartup]
internal static class TeleCoreStaticStartup
{
    static TeleCoreStaticStartup()
    {
        TLog.Message("Startup Init...");

        //MP Hook
        TLog.Message($"Multiplayer: {(MP.enabled ? "Enabled - Adding MP hooks..." : "Disabled")}");
        if (MP.enabled)
            try
            {
                MP.RegisterAll();
            }
            catch (Exception ex)
            {
                TLog.Error($"Failed to register MP hooks: {ex.Message}");
            }

        //Process Defs after load
        ApplyDefChangesPostLoad();
        TLog.Message("Startup Finished!", TColor.Green);
    }
    
    public static List<(string TypeName, Assembly Assembly)> FindDuplicateTypes()
    {
        var types = GenTypes.AllTypes;;
        var duplicates = new List<(string TypeName, Assembly Assembly)>();
        
        foreach (var type in types)
        {
            // Get type details
            var typeName = type.FullName;
            var assembly = type.Assembly;

            duplicates.Add((typeName, assembly));           
        }

        var duplicateTypes = duplicates.GroupBy(x => x)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        // Return only the types that occur more than once
        return duplicateTypes;
    }
    
    private static void DefIDValidation()
    {
        var allDefs = LoadedModManager.RunningModsListForReading.SelectMany(pack => pack.AllDefs);
        //Runs ID check
        var failed = false;
        foreach (var def in allDefs)
        {
            var toID = def.ToID();
            var andBack = toID.ToDef<Def>();
            if (andBack.ToID() != toID)
            {
                TLog.Warning($"Checking {def} failed: ({def}){toID} != ({andBack}){andBack.ToID()}");
                failed = true;
            }
        }

        if (failed) 
            TLog.Warning("Def ID check failed!");
        else
        {
            TLog.DebugSuccess("Successfully validated all Def IDs!");
        }
    }

    private static void ApplyDefChangesPostLoad()
    {
        //Load Translation Libraries
        //LoadStaticTranslationLibraries();

        DefIDValidation();

        //
        var allInjectors = DefInjectors()?.ToArray();
        TLog.Debug("Executing Def Injectors...");
        foreach (var injector in allInjectors)
        {
            TLog.Debug($"[Injector] {injector.GetType().Name}");
        }
        
        var skipInjectors = allInjectors is not {Length: > 0};
        var defs = LoadedModManager.RunningMods.SelectMany(s => s.AllDefs).ToArray();
        //TODO: Evaluate
        //TLog.Message($"Def ID Database check - Loaded IDs: {DefIDStack._MasterID} == {defs.Length}: {defs.Length - 1 == DefIDStack._MasterID}");
        foreach (var def in defs)
        {
            var bDef = def as BuildableDef;
            var tDef = def as ThingDef;
            var isBuildable = bDef != null;
            var isThing = tDef != null;

            //
            DefExtensionCache.TryRegister(def);

            if (isBuildable)
                if (bDef.HasSubMenuExtension(out var extension))
                    SubMenuThingDefList.Add(bDef, extension);

            if (skipInjectors) continue;
            foreach (var injector in allInjectors)
            {
                if (injector.AcceptsSpecial(def)) injector.OnDefSpecialInjected(def);

                if (isBuildable) injector.OnBuildableDefInject(bDef);

                if (isThing)
                {
                    //Injections
                    injector.OnThingDefInject(tDef);

                    //Pawn Check
                    if (tDef?.thingClass != null && (tDef.thingClass == typeof(Pawn) || tDef.thingClass.IsSubclassOf(typeof(Pawn))))
                    {
                        tDef.comps ??= new List<CompProperties>();
                        injector.OnPawnInject(tDef);
                    }
                }
            }
        }

        if (skipInjectors) return;
        foreach (var injector in allInjectors) injector.Dispose();
    }

    //
    private static IEnumerable<DefInjectBase> DefInjectors()
    {
        return typeof(DefInjectBase).AllSubclassesNonAbstract()
            .Select(type => (DefInjectBase) Activator.CreateInstance(type));
    }

    /*
    internal static void LoadStaticTranslationLibraries()
    {
        foreach (var type in GenTypes.AllTypesWithAttribute<StaticTranslationLibraryAttribute>())
        {
            try
            {
                TLog.Debug("Loading type for tranlsation..");
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
            catch (Exception ex)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Error in static translation library of ",
                    type,
                    ": ",
                    ex
                }));
            }
        }
    }
    */
}