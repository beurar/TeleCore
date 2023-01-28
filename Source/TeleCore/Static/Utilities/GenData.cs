using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public static class GenData
    {
        public static bool IsIncendiary(this Verb verb)
        {
            return verb.IsIncendiary_Melee() || verb.IsIncendiary_Ranged();
        }
        
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

        public static T ObjectValue<T>(this XmlNode node, bool doPostLoad = true)
        {
            return DirectXmlToObject.ObjectFromXml<T>(node, doPostLoad);
        }

        public static bool IsCustomLinked(this Graphic graphic)
        {
            if (graphic is Graphic_LinkedWithSame or Graphic_LinkedNetworkStructure)
            {
                return true;
            }
            return false;
        }

        public static bool CheckIfAnonymousType(this Type type, out bool isDisplayClass)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            bool hasCompilerGeneratedAttribute = Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false);
            //bool isGeneric = type.IsGenericType;
            bool hasCompilerStrings = (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"));
            bool hasFlags = type.Attributes.HasFlag(TypeAttributes.NotPublic);
            
            TLog.Debug($"{hasCompilerGeneratedAttribute} && {hasCompilerStrings} && {hasFlags} || {type.Name.Contains("DisplayClass")} | {type.IsGenericType}");
            isDisplayClass = type.Name.Contains("DisplayClass");;
            return hasCompilerGeneratedAttribute && hasCompilerStrings && hasFlags;
        }

        /// <summary>
        /// Registers an action to be ticked every single tick.
        /// </summary>
        public static void RegisterTickAction(this Action action)
        {
            TeleUpdateManager.Notify_AddNewTickAction(action);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void AddTaggedAction(this Action action, TeleUpdateManager.TaggedActionType type, string tag)
        {
            TeleUpdateManager.Notify_AddTaggedAction(type, action, tag);
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

        /// <summary>
        /// 
        /// </summary>
        public static Room NeighborRoomOf(this Building building, Room room)
        {
            for (int i = 0; i < 4; i++)
            {
                var cell = GenAdj.CardinalDirections[i] + building.Position;
                var roomAt = cell.GetRoom(building.Map);
                if (roomAt == room)
                {
                    Rot4 rotOfRoom = new Rot4(i);
                    var otherSide = rotOfRoom.Opposite.FacingCell + building.Position;
                    var otherRoom = otherSide.GetRoom(building.Map);
                    return otherRoom;
                }
                
            }
            return null;
        }

        /// <summary>
        /// Returns the current room at a position.
        /// </summary>
        public static Room? GetRoomFast(this IntVec3 pos, Map map)
        {
            Region validRegion = map.regionGrid.GetValidRegionAt_NoRebuild(pos);
            if (validRegion != null && validRegion.type.Passable())
            {
                return validRegion.Room;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Room? GetRoomIndirect(this Thing thing)
        {
            var room = thing.GetRoom();
            if (room == null)
            {
                room = thing.CellsAdjacent8WayAndInside().Select(c => c.GetRoom(thing.Map)).First(r => r != null);
            }
            return room;
        }

        /// <summary>
        /// Get the desired <see cref="MapInformation"/> based on type of <typeparamref name="T"/>.
        /// </summary>
        public static T GetMapInfo<T>(this Map map) where T : MapInformation
        {
            return map.TeleCore().GetMapInfo<T>();
        }

        /// <summary>
        /// Get the desired <see cref="Designator"/> based on type of <typeparamref name="T"/>.
        /// </summary>
        public static T GetDesignatorFor<T>(ThingDef def) where T : Designator
        {
            if (StaticData.cachedDesignators.TryGetValue(def, out var des))
            {
                return (T)des;
            }

            des = (Designator)Activator.CreateInstance(typeof(T), def);
            des.icon = def.uiIcon;
            StaticData.cachedDesignators.Add(def, des);
            return (T)des;
        }

        //Room Tracking
        /// <returns>The main <see cref="TeleCore.RoomTracker"/> object of the <paramref name="room"/>.</returns>
        public static RoomTracker RoomTracker(this Room room)
        {
            return room.Map.GetMapInfo<RoomTrackerMapInfo>()[room];
        }

        /// <summary>
        /// Get the desired <see cref="RoomComponent"/> based on type of <typeparamref name="T"/>.
        /// </summary>
        public static T? GetRoomComp<T>(this Room room) where T : RoomComponent
        {
            return room.RoomTracker()?.GetRoomComp<T>();
        }

        public static IEnumerable<Thing> OfType<T>(this ListerThings lister) where T : Thing
        {
            return lister.AllThings.Where(t => t is T);
        }

        //
        public static bool IsMetallic(this Thing thing)
        {
            if (thing.def.MadeFromStuff && thing.Stuff.IsMetal) return true;
            return thing.def.IsMetallic();
        }

        public static bool IsMetallic(this ThingDef def)
        {
            if (def.costList.NullOrEmpty()) return false;
            float totalCost = def.costList.Sum(t => t.count);
            float metalCost = def.costList.Find(t => t.thingDef.IsMetal)?.count ?? 0;
            return metalCost / totalCost > 0.5f;
        }

        public static bool IsBuilding(this ThingDef def)
        {
            return def.category == ThingCategory.Building;
        }

        public static bool IsWall(this ThingDef def)
        {
            if (def.category != ThingCategory.Building) return false;
            if (!def.graphicData?.Linked ?? true) return false;
            return (def.graphicData.linkFlags & LinkFlags.Wall) != LinkFlags.None &&
                   def.graphicData.linkType == LinkDrawerType.CornerFiller &&
                   def.fillPercent >= 1f &&
                   def.blockWind &&
                   def.coversFloor &&
                   def.castEdgeShadows &&
                   def.holdsRoof &&
                   def.blockLight;
        }

        public static bool TryGetComp<T>(this Thing thing, out T comp) where T : ThingComp
        {
            comp = null;
            if (thing is ThingWithComps thingWComps)
            {
                comp = thingWComps.GetComp<T>();
                return comp != null;
            }
            return false;
        }

        public static bool TryGetNetworkPart(this Thing thing, NetworkDef def, out INetworkSubPart subPart)
        {
            subPart = null;
            var networkComp = thing.TryGetComp<Comp_NetworkStructure>();
            if (networkComp == null) return false;
            subPart = networkComp[def];
            return subPart != null;
        }
        
        //
        public static List<T> ToSingleItemList<T>(this T item)
        {
            return new List<T>() {item};
        }
    }
}
