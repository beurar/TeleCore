using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore;

public class RoomTracker
{
    //Statics
    internal static readonly List<Type> RoomComponentTypes;

    private static readonly char check = '✓';
    private static readonly char fail = '❌';

    //
    private readonly HashSet<RoomTracker> adjacentTrackers;
    private readonly List<RoomComponent> comps = new();
    private readonly Dictionary<Type, RoomComponent> compsByType = new();
    private readonly List<RoomPortal> roomPortals;

    //Shared Room Data
    private readonly HashSet<Thing> uniqueContainedThingsSet = new();

    private bool _wasOutSide;

    private HashSet<IntVec3> borderCells = new();

    private HashSet<IntVec3> thinRoofCells = new();

    static RoomTracker()
    {
        RoomComponentTypes = typeof(RoomComponent).AllSubclassesNonAbstract();
    }

    public RoomTracker(Room room)
    {
        Room = room;
        ListerThings = new ListerThings(ListerThingsUse.Region);
        BorderListerThings = new ListerThings(ListerThingsUse.Region);

        //
        adjacentTrackers = new HashSet<RoomTracker>();
        roomPortals = new List<RoomPortal>();

        //Get Group Data
        UpdateGroupData();

        //Create Components
        foreach (var type in RoomComponentTypes)
        {
            var comp = (RoomComponent) Activator.CreateInstance(type);
            comp.Create(this);
            compsByType.Add(type, comp);
            comps.Add(comp);
        }

        foreach (var comp in comps) comp.PostCreate(this);
    }

    public Map Map { get; private set; }
    public bool IsDisbanded { get; private set; }
    public bool IsOutside { get; private set; }
    public bool IsProper { get; private set; }
    public int CellCount { get; private set; }
    public int OpenRoofCount { get; private set; }

    public Room Room { get; }

    public ListerThings ListerThings { get; }

    public ListerThings BorderListerThings { get; }

    public IReadOnlyCollection<Thing> ContainedPawns => ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);

    public IReadOnlyCollection<RoomComponent> AllComps => comps;
    public IReadOnlyCollection<RoomTracker> AdjacentTrackers => adjacentTrackers;
    public IReadOnlyCollection<RoomPortal> RoomPortals => roomPortals;

    public RoomPortal SelfPortal
    {
        get;
        private set;
    }

    public RegionType RegionTypes { get; private set; }

    public IReadOnlyCollection<IntVec3> BorderCellsNoCorners => borderCells;
    public IReadOnlyCollection<IntVec3> ThinRoofCells => thinRoofCells;

    public IntVec3[] MinMaxCorners { get; private set; } = new IntVec3[4];

    public IntVec3 MinVec { get; private set; }

    public IntVec2 Size { get; private set; }

    public Vector3 ActualCenter { get; private set; }

    public Vector3 DrawPos { get; private set; }

    public void FinalizeMapInit()
    {
        foreach (var comp in comps) comp.FinalizeMapInit();
    }

    public T GetRoomComp<T>() where T : RoomComponent
    {
        return (T) compsByType[typeof(T)];
    }

    public void MarkDisbanded()
    {
        IsDisbanded = true;
    }

    public void Disband(Map onMap)
    {
        adjacentTrackers.Clear();
        roomPortals.Clear();

        foreach (var comp in comps)
        {
            comp.DisbandInternal();
            comp.Disband(this, onMap);
        }
    }

    public void Notify_RegisterThing(Thing thing)
    {
        //Things
        ListerThings.Add(thing);
        foreach (var comp in comps)
        {
            comp.Notify_ThingAdded(thing);
            if (thing is Pawn pawn)
                //Entered room
                //var followerExtra = pawn.GetComp<Comp_PathFollowerExtra>();
                //followerExtra?.Notify_EnteredRoom(this);
                comp.Notify_PawnEnteredRoom(pawn);
        }
    }

    public void Notify_RegisterBorderThing(Thing thing)
    {
        //Register Portals
        if (thing is Building_Door door)
        {
            var otherRoom = door.NeighborRoomOf(Room);
            if (otherRoom == null) return;
            var otherTracker = otherRoom.RoomTracker();

            var portal = new RoomPortal(door, this, otherTracker, door.GetRoom().RoomTracker());
            adjacentTrackers.Add(otherTracker);
            roomPortals.Add(portal);
        }

        BorderListerThings.Add(thing);
        foreach (var comp in comps)
        {
            comp.Notify_BorderThingAdded(thing);
            adjacentTrackers.Do(a => comp.AddAdjacent(a.compsByType[comp.GetType()]));
        }
    }

    public void Notify_DeregisterThing(Thing thing)
    {
        //Things
        ListerThings.Remove(thing);
        foreach (var comp in comps) comp.Notify_ThingRemoved(thing);

        //Pawns
        if (thing is Pawn pawn) DeregisterPawn(pawn);
    }

    protected void DeregisterPawn(Pawn pawn)
    {
        foreach (var comp in comps) comp.Notify_PawnLeftRoom(pawn);
    }

    public bool ContainsRegionType(RegionType type)
    {
        return RegionTypes.HasFlag(type);
    }

    public bool ContainsPawn(Pawn pawn)
    {
        return ContainedPawns.Contains(pawn);
    }

    public void Reset()
    {
        adjacentTrackers.Clear();
        roomPortals.Clear();

        foreach (var comp in comps)
            comp.Reset();
    }

    public void Notify_Reused()
    {
        RegenerateData(true);
        foreach (var comp in comps) 
            comp.Notify_Reused();
    }

    public void Init(RoomTracker?[] previous)
    {
        //Try get self portal
        if (Room.IsDoorway)
        {
            var door = Room.Regions[0].door;
            var roomFacing = (door.Rotation.FacingCell + door.Position).GetRoom(Map)?.RoomTracker();
            var roomOpposite = (door.Rotation.Opposite.FacingCell + door.Position).GetRoom(Map)?.RoomTracker();
            if (roomFacing == null || roomOpposite == null)
            {
                SelfPortal = new RoomPortal(door, this);
            }
            else
            {
                SelfPortal = new RoomPortal(Room.Regions[0].door, roomFacing,roomOpposite, this);
            }
        }
        
        //
        RegenerateData();
        foreach (var comp in comps) 
            comp.Init(previous);
    }

    public void PostInit(RoomTracker?[] previous)
    {
        foreach (var comp in comps)
            comp.PostInit(previous);
    }

    public void Notify_RoofChanged()
    {
        RegenerateData(true, false, false);
        //Check if room closed
        if (_wasOutSide && !IsOutside) RoofClosed();

        if (!_wasOutSide && IsOutside) RoofOpened();

        foreach (var comp in comps) comp.Notify_RoofChanged();
    }

    private void RoofClosed()
    {
        foreach (var comp in comps) comp.Notify_RoofClosed();
    }

    private void RoofOpened()
    {
        foreach (var comp in comps) comp.Notify_RoofOpened();
    }

    public void RoomTick()
    {
        foreach (var comp in comps) comp.CompTick();
    }

    public void RoomOnGUI()
    {
        foreach (var comp in comps) comp.OnGUI();
    }

    public void RoomDraw()
    {
        foreach (var comp in comps) comp.Draw();
    }

    private string Icon(bool checkFail)
    {
        return $"{(checkFail ? check : fail)}";
    }

    private Color ColorSel(bool checkFail)
    {
        return checkFail ? Color.green : Color.red;
    }

    public void Validate()
    {
        TLog.Message($"### Validating Tracker[{Room.ID}]");
        var innerCells = Room.Cells;

        var containedThing = Room.ContainedAndAdjacentThings.Where(t => innerCells.Contains(t.Position)).ToList();
        var sameCount = containedThing.Count(t => ListerThings.Contains(t));
        var sameRatio = sameCount / (float) containedThing.Count();
        var sameBool = sameRatio >= 1f;
        var sameRatioString =
            $"[{sameCount}/{containedThing.Count()}][{sameRatio}]{Icon(sameBool)}".Colorize(ColorSel(sameBool));
        TLog.Message($"ContainedThingRatio: {sameRatioString}");
        TLog.Message($"### Ending Validation [{Room.ID}]");
    }

    public void RegenerateData(bool ignoreRoomExtents = false, bool regenCellData = true, bool regenListerThings = true)
    {
        var stepCounter = 0;
        try
        {
            UpdateGroupData();
            stepCounter = 1;
            if (!ignoreRoomExtents)
            {
                if (Room.Dereferenced)
                {
                    TLog.Warning($"Trying to regenerate dereferenced room. IsDisbanded: {IsDisbanded}");
                    return;
                }

                var extents = Room.ExtentsClose;
                var minX = extents.minX;
                var maxX = extents.maxX;
                var minZ = extents.minZ;
                var maxZ = extents.maxZ;
                MinMaxCorners = extents.Corners.ToArray();

                stepCounter = 2;
                MinVec = new IntVec3(minX, 0, minZ);
                Size = new IntVec2(maxX - minX + 1, maxZ - minZ + 1);
                ActualCenter = extents.CenterVector3;
                DrawPos = new Vector3(minX, AltitudeLayer.FogOfWar.AltitudeFor(), minZ);
                stepCounter = 3;
            }

            //Get Roof and Border Cells
            if (regenCellData)
            {
                stepCounter = 4;
                GenerateCellData();
            }

            stepCounter = 5;
            //Get ListerThings
            if (regenListerThings)
            {
                stepCounter = 6;
                ListerThings.Clear();
                BorderListerThings.Clear();
                //TODO: Main Performance Killer - Thing processing, maybe parallel for?
                List<Region> regions = Room.Regions;
                stepCounter = 7;

                void ProcessItem(Thing item)
                {
                    if (item.Position.GetRoomFast(Map) != Room)
                    {
                        if (uniqueContainedThingsSet.Add(item)) Notify_RegisterBorderThing(item);
                        return;
                    }

                    if (IsOutside) return;

                    if (uniqueContainedThingsSet.Add(item)) Notify_RegisterThing(item);
                }

                //TODO: Research viability of parallel
                //var allThingsRegions = regions.SelectMany(r => r.ListerThings.AllThings);
                //Parallel.ForEach(allThingsRegions, ProcessItem);

                for (var i = 0; i < regions.Count; i++)
                {
                    List<Thing> allThings = regions[i].ListerThings.AllThings;
                    if (allThings != null)
                        for (var j = 0; j < allThings.Count; j++)
                        {
                            var item = allThings[j];
                            if (item.Position.GetRoomFast(Map) != Room)
                            {
                                if (uniqueContainedThingsSet.Add(item)) Notify_RegisterBorderThing(item);
                                continue;
                            }

                            if (IsOutside) continue;
                            if (uniqueContainedThingsSet.Add(item)) Notify_RegisterThing(item);
                        }
                }

                stepCounter = 8;
                uniqueContainedThingsSet.Clear();
            }
        }
        catch (Exception ex)
        {
            if (ex is OverflowException oEx)
                TLog.Error($"Arithmetic Overflow Exception in RegenerateData - reached step {stepCounter}: {oEx}");
        }
    }

    private void GenerateCellData()
    {
        if (!IsProper) return;

        var tCells = new HashSet<IntVec3>();
        var bCells = new HashSet<IntVec3>();
        foreach (var c in Room.Cells)
        {
            if (!Map.roofGrid.RoofAt(c)?.isThickRoof ?? false)
                tCells.Add(c);

            for (var i = 0; i < 4; i++)
            {
                var cardinal = c + GenAdj.CardinalDirections[i];

                var region = cardinal.GetRegion(Map);
                if ((region == null || region.Room != Room) && cardinal.InBounds(Map)) bCells.Add(cardinal);
            }
        }

        borderCells = bCells;
        thinRoofCells = tCells;
    }

    //Cache Room-Data incase the Room is dereferenced.
    private void UpdateGroupData()
    {
        //
        _wasOutSide = IsOutside;
        IsOutside = Room.UsesOutdoorTemperature; //Caching to avoid recalc
        IsProper = Room.ProperRoom;

        Map = Room.Map;
        CellCount = Room.CellCount;

        //
        if (!IsOutside)
            //If not outside, we want to know if there are any open roof cells (implies: small room with a few open roof cells
            OpenRoofCount = Room.OpenRoofCount;
        foreach (var roomRegion in Room.Regions) RegionTypes |= roomRegion.type;
    }
}