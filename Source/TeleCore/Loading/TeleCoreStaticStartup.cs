using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using UnityEngine.Assertions;
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
        }

        private static void ApplyDefChangesPostLoad()
        {
            //Load Translation Libraries
            //LoadStaticTranslationLibraries();
            
            //
            var allInjectors = DefInjectors()?.ToArray();
            var skipInjectors = allInjectors is not { Length: > 0 };

            foreach (var def in DefDatabase<Def>.AllDefsListForReading)
            {
                var bDef = def as BuildableDef;
                var tDef = def as ThingDef;
                var isBuildable = bDef != null;
                var isThing = tDef != null;

                //Register ID
                StaticData.RegisterDefID(def);

                if (isBuildable)
                {
                    DefExtensionCache.TryRegister(def);
                }

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
