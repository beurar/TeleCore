using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Multiplayer.API;
using TeleCore.FlowCore;
using TeleCore.Loading.InternalTests;
using Verse;

namespace TeleCore
{
    [StaticConstructorOnStartup]
    internal static class TeleCoreStaticStartup
    {
        static TeleCoreStaticStartup()
        {
            TLog.Message("Startup Init");
            
            //MP Hook
            TLog.Message($"Multiplayer: {(MP.enabled ? "Enabled - Adding MP hooks..." : "Disabled")}");
            if (MP.enabled)
            {
                MP.RegisterAll();
            }
            
            //Process Defs after load
            ApplyDefChangesPostLoad();
            
            //Static Startup Tests
            //var prof = new Profiling();
            //prof.ContainerInitTest();
            //Aprof.ProfileTest1();
        }

        private static void ApplyDefChangesPostLoad()
        {
            //Load Translation Libraries
            //LoadStaticTranslationLibraries();
            
            //
            var allInjectors = DefInjectors()?.ToArray();
            var skipInjectors = allInjectors is not { Length: > 0 };
            
            var defs = LoadedModManager.RunningMods.SelectMany(s => s.AllDefs);
            TLog.Debug($"Defs: {defs.Count()}");
            TLog.Debug($"Defs: {DefDatabase<ThingDef>.AllDefsListForReading.Count} | {DefDatabase<ThingDef>.AllDefs.Count()} ");
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
