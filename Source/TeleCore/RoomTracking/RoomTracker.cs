using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TeleCore;

public class RoomTracker
{
    private bool wasOutSide = false;

    private Dictionary<Type, RoomComponent> compsByType = new();
    private List<RoomComponent> comps = new List<RoomComponent>();
    private List<RoomTracker> adjacentTrackers;
    private List<RoomPortal> roomPortals;

    //Shared Room Data
    private readonly HashSet<Thing> uniqueContainedThingsSet = new HashSet<Thing>();
    protected ListerThings listerThings;
    protected ListerThings borderListerThings;

    private HashSet<IntVec3> borderCells = new HashSet<IntVec3>();
    private HashSet<IntVec3> thinRoofCells = new HashSet<IntVec3>();

    private IntVec3[] cornerCells = new IntVec3[4];
    private IntVec3 minVec;
    private IntVec2 size;

    private Vector3 actualCenter;
    private Vector3 drawPos;

    private RegionType regionTypes;

    public bool IsDisbanded { get; private set; }
    public bool IsOutside { get; private set; }
    public bool IsProper { get; private set; }

    public int CellCount { get; private set; }
    public int OpenRoofCount { get; private set; }

    public Map Map { get; private set; }
    public Room Room { get; }

    public ListerThings ListerThings => listerThings;
    public ListerThings BorderListerThings => borderListerThings;
    public List<Thing> ContainedPawns => listerThings.ThingsInGroup(ThingRequestGroup.Pawn);

    public List<RoomTracker> AdjacentTrackers => adjacentTrackers;
    public List<RoomPortal> RoomPortals => roomPortals;

    public RegionType RegionTypes => regionTypes;

    public HashSet<IntVec3> BorderCellsNoCorners => borderCells;
    public HashSet<IntVec3> ThinRoofCells => thinRoofCells;

    public IntVec3[] MinMaxCorners => cornerCells;
    public IntVec3 MinVec => minVec;
    public IntVec2 Size => size;
    public Vector3 ActualCenter => actualCenter;
    public Vector3 DrawPos => drawPos;

    public RoomTracker(Room room)
    {
        Room = room;
        listerThings = new ListerThings(ListerThingsUse.Region);
        borderListerThings = new ListerThings(ListerThingsUse.Region);

        //
        adjacentTrackers = new List<RoomTracker>();
        roomPortals = new List<RoomPortal>();

        //Get Group Data
        UpdateGroupData();
        foreach (var type in typeof(RoomComponent).AllSubclassesNonAbstract())
        {
            var comp = (RoomComponent) Activator.CreateInstance(type);
            comp.Create(this);
            compsByType.Add(type, comp);
            comps.Add(comp);
        }
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
            comp.Disband(this, onMap);
        }
    }

    public void Notify_RegisterThing(Thing thing)
    {
        //Things
        listerThings.Add(thing);
        foreach (var comp in comps)
        {
            comp.Notify_ThingAdded(thing);
        }

        //Pawns
        if (thing is Pawn pawn)
        {
            RegisterPawn(pawn);
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

        borderListerThings.Add(thing);
        foreach (var comp in comps)
        {
            comp.Notify_BorderThingAdded(thing);

            //
            adjacentTrackers.Do(a => comp.AddAdjacent(a.compsByType[comp.GetType()]));
        }
    }

    public void Notify_DeregisterThing(Thing thing)
    {
        //Things
        listerThings.Remove(thing);
        foreach (var comp in comps)
        {
            comp.Notify_ThingRemoved(thing);
        }

        //Pawns
        if (thing is Pawn pawn)
        {
            DeregisterPawn(pawn);
        }
    }

    protected void RegisterPawn(Pawn pawn)
    {
        //Entered room
        //var followerExtra = pawn.GetComp<Comp_PathFollowerExtra>();
        //followerExtra?.Notify_EnteredRoom(this);

        //
        foreach (var comp in comps)
        {
            comp.Notify_PawnEnteredRoom(pawn);
        }
    }

    protected void DeregisterPawn(Pawn pawn)
    {
        //
        foreach (var comp in comps)
        {
            comp.Notify_PawnLeftRoom(pawn);
        }
    }

    public bool ContainsRegionType(RegionType type)
    {
        return regionTypes.HasFlag(type);
    }

    public bool ContainsPawn(Pawn pawn)
    {
        return ContainedPawns.Contains(pawn);
    }

    //
    public void Reset()
    {
        adjacentTrackers.Clear();
        roomPortals.Clear();

        //
        foreach (var comp in comps)
        {
            comp.Reset();
        }
    }

    public void Notify_Reused()
    {
        //
        RegenerateData(true);
        TProfiler.Check("Reused_1");
        foreach (var comp in comps)
        {
            comp.Notify_Reused();
        }
        TProfiler.Check("Reused_2");
    }

    public void PreApply()
    {
        RegenerateData();
        foreach (var comp in comps)
        {
            comp.PreApply();
        }
    }

    public void FinalizeApply()
    {
        foreach (var comp in comps)
        {
            comp.FinalizeApply();
            TProfiler.Check($"Finalized {comp}");
        }
    }

    public void Notify_RoofChanged()
    {
        RegenerateData(true, false, false);
        //Check if room closed
        if (wasOutSide && !IsOutside)
        {
            RoofClosed();
        }

        if (!wasOutSide && IsOutside)
        {
            RoofOpened();
        }

        foreach (var comp in comps)
        {
            comp.Notify_RoofChanged();
        }
    }

    private void RoofClosed()
    {
        foreach (var comp in comps)
        {
            comp.Notify_RoofClosed();
        }
    }

    private void RoofOpened()
    {
        foreach (var comp in comps)
        {
            comp.Notify_RoofOpened();
        }
    }

    public void RoomTick()
    {
        foreach (var comp in comps)
        {
            comp.CompTick();
        }
    }

    public void RoomOnGUI()
    {
        foreach (var comp in comps)
        {
            comp.OnGUI();
        }
    }

    public void RoomDraw()
    {
        foreach (var comp in comps)
        {
            comp.Draw();
        }
    }

    private static char check = '✓';
    private static char fail = '❌';

    private string Icon(bool checkFail) => $"{(checkFail ? check : fail)}";
    private Color ColorSel(bool checkFail) => (checkFail ? Color.green : Color.red);

    public void Validate()
    {
        TLog.Message($"### Validating Tracker[{Room.ID}]");
        var innerCells = Room.Cells;

        var containedThing = Room.ContainedAndAdjacentThings.Where(t => innerCells.Contains(t.Position)).ToList();
        int sameCount = containedThing.Count(t => ListerThings.Contains(t));
        float sameRatio = sameCount / (float) containedThing.Count();
        bool sameBool = sameRatio >= 1f;
        var sameRatioString =
            $"[{sameCount}/{containedThing.Count()}][{sameRatio}]{Icon(sameBool)}".Colorize(ColorSel(sameBool));
        TLog.Message($"ContainedThingRatio: {sameRatioString}");
        TLog.Message($"### Ending Validation [{Room.ID}]");
    }

    public void RegenerateData(bool ignoreRoomExtents = false, bool regenCellData = true, bool regenListerThings = true)
    {
        try
        {
            UpdateGroupData();
            TProfiler.Check("Regen_1");
            if (!ignoreRoomExtents)
            {
                var extents = Room.ExtentsClose;
                int minX = extents.minX;
                int maxX = extents.maxX;
                int minZ = extents.minZ;
                int maxZ = extents.maxZ;
                cornerCells = extents.Corners.ToArray();

                minVec = new IntVec3(minX, 0, minZ);
                size = new IntVec2(maxX - minX + 1, maxZ - minZ + 1);
                actualCenter = extents.CenterVector3;
                drawPos = new Vector3(minX, AltitudeLayer.FogOfWar.AltitudeFor(), minZ);
            }
            
            TProfiler.Check("Regen_2");
            //Get Roof and Border Cells
            if (regenCellData)
            {
                GenerateCellData();
            }

            TProfiler.Check("Regen_3");
            //Get ListerThings
            if (regenListerThings)
            {
                listerThings.Clear();
                borderListerThings.Clear();
                //TODO: Main Performance Killer - Thing processing, maybe parallel for?
                List<Region> regions = Room.Regions;
                for (int i = 0; i < regions.Count; i++)
                {
                    List<Thing> allThings = regions[i].ListerThings.AllThings;
                    if (allThings != null)
                    {
                        for (int j = 0; j < allThings.Count; j++)
                        {
                            Thing item = allThings[j];
                            if (item.Position.GetRoomFast(Map) != Room)
                            {
                                if (uniqueContainedThingsSet.Add(item))
                                {
                                    Notify_RegisterBorderThing(item);
                                }
                                continue;
                            }

                            if (IsOutside)
                            {
                                continue;
                            }
                            if (uniqueContainedThingsSet.Add(item))
                            {
                                Notify_RegisterThing(item);
                            }
                        }
                    }
                }
                uniqueContainedThingsSet.Clear();
            }
            TProfiler.Check("Regen_4");
        }
        catch (Exception ex)
        {
            if (ex is OverflowException oEx)
            {
                TLog.Error($"Arithmetic Overflow Exception in RegenerateData: {oEx}");
            }
        }
    }

    private void GenerateCellData()
    {
        if (!IsProper) return;

        var tCells = new HashSet<IntVec3>();
        var bCells = new HashSet<IntVec3>();
        foreach (IntVec3 c in Room.Cells)
        {
            if (!Map.roofGrid.RoofAt(c)?.isThickRoof ?? false)
                tCells.Add(c);

            for (int i = 0; i < 4; i++)
            {
                IntVec3 cardinal = c + GenAdj.CardinalDirections[i];

                var region = cardinal.GetRegion(Map);
                if ((region == null || region.Room != Room) && cardinal.InBounds(Map))
                {
                    bCells.Add(cardinal);
                }
            }
        }

        borderCells = bCells;
        thinRoofCells = tCells;
    }

    private void UpdateGroupData()
    {
        //
        wasOutSide = IsOutside;
        IsOutside = Room.UsesOutdoorTemperature;
        IsProper = Room.ProperRoom;

        //
        Map = Room.Map;
        CellCount = Room.CellCount;

        //
        if (!IsOutside)
        {
            //If not outside, we want to know if there are any open roof cells (implies: small room with a few open roof cells
            OpenRoofCount = Room.OpenRoofCount;
        }

        //
        foreach (var roomRegion in Room.Regions)
        {
            regionTypes |= roomRegion.type;
        }
    }
}

