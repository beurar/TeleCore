using System;
using System.Collections.Generic;
using TeleCore.Data.Events;
using UnityEngine;
using Verse;

namespace TeleCore;

/// <summary>
/// Handles additional pathing data, such as avoid weights, walkable/wander cells and danger 
/// </summary>
public class PathHelperInfo : MapInformation
{
    public readonly HashSet<AvoidGridWorker> workers;
    
    public PathHelperInfo(Map map) : base(map)
    {
        workers = new HashSet<AvoidGridWorker>();
        GlobalEventHandler.CellChanged += Notify_CellChanged;
    }

    public override void InfoInit(bool initAfterReload = false)
    {
        base.InfoInit(initAfterReload);
        foreach (var def in DefDatabase<AvoidGridDef>.AllDefsListForReading)
        {
            var worker = (AvoidGridWorker)Activator.CreateInstance(def.avoidGridClass, map, def);
            workers.Add(worker);
        }
    }

    private void Notify_CellChanged(CellChangedEventArgs args)
    {
        foreach (var worker in workers)
        {
            worker.Notify_CellChanged(args);
        }
    }

    private static readonly int SampleNumCells = GenRadial.NumCellsInRadius(8.9f);

    public override void UpdateOnGUI()
    {
        //Debug Rendering
        if (!PlaySettingsAvoidGrid.DrawAvoidGridsAroundMouse) return;
        
        var root = UI.MouseCell();
        var map = Find.CurrentMap;
        for (int i = 0; i < SampleNumCells; i++)
        {
            IntVec3 intVec = root + GenRadial.RadialPattern[i];
            if (intVec.InBounds(map) && !intVec.Fogged(map))
            {
                var index = intVec.Index(map);
                var pathGrid = map.pathFinder.pathGrid.pathGrid[index];
                var baseVal = map.avoidGrid.grid[index];
                int newVal = 0;
                foreach (var worker in workers)
                {
                    newVal += worker.Grid[index];
                }

                var pos = GenMapUI.LabelDrawPosFor(intVec);
                GenMapUI.DrawThingLabel(pos + new Vector2(0, 32), newVal.ToString(), Color.cyan);
                GenMapUI.DrawThingLabel(pos + new Vector2(0, 0f), baseVal.ToString(), Color.green);
                GenMapUI.DrawThingLabel(pos + new Vector2(0, -32f), pathGrid.ToString(), Color.red);
            }
        }
    }
}