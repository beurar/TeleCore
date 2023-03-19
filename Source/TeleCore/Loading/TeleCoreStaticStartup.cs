using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Multiplayer.API;
using RimWorld;
using TeleCore.FlowCore;
using TeleCore.Loading.InternalTests;
using TeleCore.Memory;
using UnityEngine;
using Verse;

namespace TeleCore
{
    [StaticConstructorOnStartup]
    internal static class TeleCoreStaticStartup
    {
        static TeleCoreStaticStartup()
        {
            using var garbo = new GarbageMan(); 
            TLog.Message("Startup Init...");
            
            //MP Hook
            TLog.Message($"Multiplayer: {(MP.enabled ? "Enabled - Adding MP hooks..." : "Disabled")}");
            if (MP.enabled)
            {
                MP.RegisterAll();
            }
            //Process Defs after load
            ApplyDefChangesPostLoad();
            
            TLog.Message("Startup Finished!", TColor.Green);
        }

        private static void DefIDCheck()
        {
            var allDefs = LoadedModManager.RunningModsListForReading.SelectMany(pack => pack.AllDefs);
            //Runs ID check
            bool failed = false;
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
            {
                TLog.Warning("Def ID check failed!");
            }
        }

        private static void ApplyDefChangesPostLoad()
        {
            //Load Translation Libraries
            //LoadStaticTranslationLibraries();

            DefIDCheck();
            
            //
            var allInjectors = DefInjectors()?.ToArray();
            var skipInjectors = allInjectors is not { Length: > 0 };
            
            var defs = LoadedModManager.RunningMods.SelectMany(s => s.AllDefs).ToArray();
            TLog.Message($"Def ID Database check - Loaded IDs: {DefIDStack._MasterID} == {defs.Length}: {defs.Length - 1 == DefIDStack._MasterID}");
            foreach (var def in defs)
            {
                var bDef = def as BuildableDef;
                var tDef = def as ThingDef;
                var isBuildable = bDef != null;
                var isThing = tDef != null;
 
                //
                DefExtensionCache.TryRegister(def);

                if (isThing)
                {
                    if (tDef.HasSubMenuExtension(out var extension))
                    {
                        SubMenuThingDefList.Add(tDef, extension);
                    }
                }

                if (skipInjectors) continue;
                foreach (var injector in allInjectors)
                {
                    if (isBuildable)
                    {
                        injector.OnBuildableDefInject(bDef);
                    }

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
            foreach (var injector in allInjectors)
            {
                injector.Dispose();
            }
        }

        //
        private static IEnumerable<DefInjectBase> DefInjectors()
        {
            return typeof(DefInjectBase).AllSubclassesNonAbstract().Select(type => (DefInjectBase)Activator.CreateInstance(type));
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
}
