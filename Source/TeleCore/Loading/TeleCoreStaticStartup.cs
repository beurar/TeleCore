using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
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
            
            //
            ApplyDefChangesPostLoad();
        }

        internal static void ApplyDefChangesPostLoad()
        {
            //Load Translation Libraries
            //LoadStaticTranslationLibraries();
            
            //
            var allInjectors = DefInjectors()?.ToArray();

            //All Buildables
            foreach (var def in DefDatabase<BuildableDef>.AllDefsListForReading)
            {
                DefExtensionCache.TryRegister(def);

                //
                if (allInjectors == null) continue;
                foreach (var injector in allInjectors)
                {
                    injector.OnBuildableDefInject(def);
                }
            }

            //All Pawns
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                //
                //Sub Menu
                if (def.HasSubMenuExtension(out var extension))
                {
                    SubMenuThingDefList.Add(def, extension);
                }
                
                if (allInjectors == null) continue;
                foreach (var injector in allInjectors)
                {
                    //Injections
                    injector.OnThingDefInject(def);

                    //Pawn Check
                    if (def?.thingClass == null) continue;
                    Type thingClass = def.thingClass;
                    if (!thingClass.IsSubclassOf(typeof(Pawn)) && thingClass != typeof(Pawn)) continue;
                    if (def.comps == null)
                        def.comps = new List<CompProperties>();

                    injector.OnPawnInject(def);
                }
            }

            if (allInjectors == null) return;
            foreach (var injector in allInjectors)
            {
                injector.Dispose();
            }
        }

        //
        private static IEnumerable<DefInjectBase> DefInjectors()
        {
            var allSubclasses = typeof(DefInjectBase).AllSubclassesNonAbstract();
            if (allSubclasses.Any())
            {
                return allSubclasses.Select(t => (DefInjectBase)Activator.CreateInstance(t));
            }
            return null;
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
