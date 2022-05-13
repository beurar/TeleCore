using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public static class GenData
    {
        public static string Location(this Texture texture)
        {
            if (texture is not Texture2D tx2D)
            {
                TLog.Error($"Tried to find {texture} location as non Texture2D");
                return null;
            }
            return LoadedModManager.RunningMods.SelectMany(m => m.textures.contentList).First(t => t.Value == tx2D).Key;
        }

        public static string Location(this Shader shader)
        {
            return DefDatabase<ShaderTypeDef>.AllDefs.First(t => t.Shader == shader).shaderPath;
        }

        public static IEnumerable<T> AllFlags<T>(this T enumType) where T : Enum
        {
            return enumType.GetAllSelectedItems<T>();
        }

        //
        /// <summary>
        /// Registers an action to be ticked every single tick.
        /// </summary>
        public static void RegisterTickAction(this Action action)
        {
            TeleUpdateManager.Notify_AddNewTickAction(action);
        }

        /// <summary>
        /// Enqueues an action to be run once on the main thread when available.
        /// </summary>
        public static void EnqueueActionForMainThread(this Action action)
        {
            TeleUpdateManager.Notify_EnqueueNewSingleAction(action);
        }

        /// <summary>
        /// Defines whether a structure is powered by electricity and returns whether it actually uses power.
        /// </summary>
        public static bool IsElectricallyPowered(this ThingWithComps thing, out bool usesPower)
        {
            var comp = thing.GetComp<CompPowerTrader>();
            usesPower = comp != null;
            return usesPower && comp.PowerOn;
        }
        
        /// <summary>
        /// If the thing uses a PowerComp, returns the PowerOn property, otherwise returns true if no PowerComp exists.
        /// </summary>
        public static bool IsPoweredOn(this ThingWithComps thing)
        {
            return thing.IsElectricallyPowered(out var usesPower) || !usesPower;
        }

        /// <summary>
        /// Checks whether a thing is reserved by any pawn.
        /// </summary>
        public static bool IsReserved(this Thing thing, Map onMap, out Pawn reservedBy)
        {
            reservedBy = null;
            if (thing == null) return false;
            var reservations = onMap.reservationManager;
            reservedBy = reservations.ReservationsReadOnly.Find(r => r.Target == thing)?.Claimant;
            return reservedBy != null;
        }
    }
}
