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
        
        public static void Foo()
        {
            DefValueStack<ThingDef> stack1 = new DefValueStack<ThingDef>(new DefFloat<ThingDef>[2]
            {
                new DefFloat<ThingDef>(ThingDefOf.Steel, 10),
                new DefFloat<ThingDef>(ThingDefOf.Gold, 10),
            });
            DefValueStack<ThingDef> stack2 = new DefValueStack<ThingDef>(new DefFloat<ThingDef>[2]
            {
                new DefFloat<ThingDef>(ThingDefOf.Steel, 22),
                new DefFloat<ThingDef>(ThingDefOf.Gold, 22),
            });

            var newStack = stack1 + stack2;
            
            TLog.Debug($"\nS1: {stack1}\nS2: {stack2} \nSN: {newStack}");
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
