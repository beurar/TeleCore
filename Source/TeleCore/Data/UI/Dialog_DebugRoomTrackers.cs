using UnityEngine;
using Verse;

namespace TeleCore.UI;

public class Dialog_DebugRoomTrackers : Window
{
    private Vector2 _selScrollPos;
    private Vector2 _selScrollPos2;
    
    private Map Map => Find.CurrentMap;
    private MapInformation_Rooms Rooms => Map?.GetMapInfo<MapInformation_Rooms>();

    private RoomTracker SelTracker { get; set; }
    private RoomComponent SelComponent { get; set; }
    
    public override void DoWindowContents(Rect inRect)
    {
        inRect = inRect.ContractedBy(5);
        var leftRect = inRect.LeftPart(0.40f);
        var metaRect = leftRect.TopPart(0.25f);

        var selRect = leftRect.BottomPart(0.75f);
        var trackerSelection = selRect.LeftHalf();
        var compSelection = selRect.RightHalf();
        var dataRect = inRect.RightPart(0.60f);
        
        //Write Data
        Widgets.DrawMenuSection(metaRect);
        metaRect = metaRect.ContractedBy(2f);
        Widgets.Label(metaRect, $"Rooms: {Map.regionGrid.allRooms.Count}");
        
        //Draw Selections
        Widgets.DrawMenuSection(selRect);
        
        //Tracker
        var curY = 5f;
        var scrollArea = new Rect(trackerSelection.x, trackerSelection.y, trackerSelection.width, Rooms.AllTrackers.Count * 24);
        Widgets.BeginScrollView(scrollArea, ref _selScrollPos, trackerSelection, false);
        {
            foreach (var tracker in Rooms.AllTrackers)
            {
                var trackerRect = new Rect(trackerSelection.x + 5, curY, trackerSelection.width - 10, 24);
                TWidgets.DrawBox(trackerRect, 0.5f, 1);
                Widgets.Label(trackerRect, $"[{tracker.Value.Room.ID}]");
                if (Widgets.ButtonInvisible(trackerRect))
                {
                    SelTracker = tracker.Value;
                }
            }
        }
        Widgets.EndScrollView();
        
        Widgets.DrawLineVertical(trackerSelection.xMax, trackerSelection.y, trackerSelection.height);
        
        if (SelTracker != null)
        {
            //Comp
            curY = 5f;
            var scrollAreaComp = new Rect(compSelection.x, compSelection.y, compSelection.width,Rooms.AllTrackers.Count * 24);
            Widgets.BeginScrollView(scrollAreaComp, ref _selScrollPos2, compSelection, false);
            {
                foreach (var component in SelTracker.Comps)
                {
                    var compRect = new Rect(compSelection.x + 5, curY, compSelection.width - 10, 24);
                    TWidgets.DrawBox(compRect, 0.5f, 1);
                    if (Widgets.ButtonInvisible(compRect))
                    {
                        SelComponent = component;
                    }
                }
            }
            Widgets.EndScrollView();
        }

        //Draw RoomComp Data
        dataRect = dataRect.ContractedBy(5);
        Widgets.DrawMenuSection(dataRect);

        if (SelTracker != null)
        {
            var neighbors = SelTracker.RoomNeighbors;
            Listing_Standard list = new Listing_Standard();
            list.Begin(dataRect.LeftHalf());
            list.Label($"ID: {SelTracker.Room.ID}");
            list.Label($"IsOutside: {SelTracker.IsOutside}");
            list.Label($"IsProper: {SelTracker.IsProper}");
            list.Label($"Corners: {SelTracker.MinMaxCorners.ToStringSafeEnumerable()}");
            list.Label($"Center: {SelTracker.ActualCenter}");
            list.Label($"Pawns: {SelTracker.ContainedPawns}");
            var trueNeighborsSize = neighbors.TrueNeighbors.Count * 24;
            list.Label($"True Neighbors: {neighbors.TrueNeighbors.Count}");
            list.BeginSection(trueNeighborsSize);
            int count = 0;
            foreach (var trueNeighbor in neighbors.TrueNeighbors)
            {
                list.Label($"[{count}]: {trueNeighbor.Room.ID}");
                count++;
            }
            list.EndSection(list);
            
            var nghbSize = neighbors.AttachedNeighbors.Count * 24;
            list.Label($"Attached Neighbors: {neighbors.AttachedNeighbors.Count}");
            list.BeginSection(nghbSize);
            count = 0;
            foreach (var attached in neighbors.AttachedNeighbors)
            {
                list.Label($"[{count}]: {attached.Room.ID}");
                count++;
            }
            list.EndSection(list);
            
            list.End();
        }
        
        if (SelComponent != null)
        {
            //Comp Data
            var neighbors = SelComponent.CompNeighbors;
        }
        
        
    }
}