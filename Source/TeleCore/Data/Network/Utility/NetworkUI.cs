using System.Collections.Generic;
using TeleCore.Defs;
using TeleCore.Generics.Container;
using TeleCore.Network.Flow;
using TeleCore.Static;
using UnityEngine;
using Verse;
using DebugTools = TeleCore.Static.Utilities.DebugTools;

namespace TeleCore.Network.Utility;

public static class NetworkUI
{
    public static Vector2 GetValueContainerReadoutSize<TValue>(ValueContainerBase<TValue> container) where TValue : FlowValueDef
    {
        Vector2 size = new Vector2(10, 10);
        foreach (var type in container.StoredDefs)
        {
            Vector2 typeSize = Text.CalcSize($"{type.labelShort}: {container.StoredValueOf(type)} ({container.StoredPercentOf(type).ToStringPercent()})");
            size.y += 10 + 2;
            var sizeX = typeSize.x + 20;
            if (size.x <= sizeX)
                size.x += sizeX;
        }
        return size;
    }

    public static Vector2 GetFlowBoxReadoutSize(Flow.FlowBox fb)
    {
        var size = new Vector2(10, 10);
        var stack = fb.Stack;
        foreach (var fv in stack.Values)
        {
            var type = fv.Def;
            //TODO: better percent calc
            Vector2 typeSize = Text.CalcSize($"{type.labelShort}: {fb.StoredValueOf(fv.Def)} ({fb.StoredPercentOf(type).ToStringPercent()})");
            size.y += 10 + 2;
            var sizeX = typeSize.x + 20;
            if (size.x <= sizeX)
                size.x += sizeX;
        }
        return size;
    }
    
    public static void DrawFlowBoxReadout(Rect rect, Flow.FlowBox fb)
    {
        float height = 5;
        Widgets.DrawMenuSection(rect);
        Widgets.BeginGroup(rect);
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperLeft;
        foreach (var fv in fb.Stack.Values)
        {
            var type = fv.Def;
            string label = $"{type.labelShort}: {fb.StoredValueOf(type)} ({fb.StoredPercentOf(type).ToStringPercent()})";
            Rect typeRect = new Rect(5, height, 10, 10);
            Vector2 typeSize = Text.CalcSize(label);
            Rect typeLabelRect = new Rect(20, height - 2, typeSize.x, typeSize.y);
            Widgets.DrawBoxSolid(typeRect, type.valueColor);
            Widgets.Label(typeLabelRect, label);
            height += 10 + 2;
        }

        Text.Font = default;
        Text.Anchor = default;
        Widgets.EndGroup();

        if (DebugSettings.godMode)
        {
            if (TWidgets.MouseClickIn(rect, 1))
            {
                FloatMenu menu = new FloatMenu(DebugFloatMenuOptions(fb), "", true);
                menu.vanishIfMouseDistant = true;
                Find.WindowStack.Add(menu);
            }
        }
    }

    public static List<FloatMenuOption> DebugFloatMenuOptions(Flow.FlowBox fb)
    {
        var tempList = StaticListHolder<FloatMenuOption>.RequestList($"FlowMenuOptions_{fb.GetHashCode()}");
        /*
        if (tempList.Count == 0)
        {
            int part = (int)(fb.MaxCapacity / fb.AcceptedTypes.Count);
            tempList.Add(new FloatMenuOption("Add ALL", delegate { Debug_AddAll(part); }));

            tempList.Add(new FloatMenuOption("Remove ALL", Debug_Clear));

            foreach (var type in fb.AcceptedTypes)
            {
                tempList.Add(new FloatMenuOption($"Add {type}", delegate { Debug_AddType(type, part); }));
            }
        }
        */
        return tempList;
    }


    /*
    public static float DrawNetworkValueTypeReadout(Rect rect, GameFont font, float textYOffset, NetworkContainerSet containerSet)
    {
        float height = 5;

        Widgets.BeginGroup(rect);
        Text.Font = font;
        Text.Anchor = TextAnchor.UpperLeft;
        foreach (var type in containerSet.AllTypes)
        {
            // float value = GetNetwork(Find.CurrentMap).NetworkValueFor(type);
            //if(value <= 0) continue;
            string label = $"{type}: {containerSet.GetValueByType(type)}";
            Rect typeRect = new Rect(5, height, 10, 10);
            Vector2 typeSize = Text.CalcSize(label);
            Rect typeLabelRect = new Rect(20, height + textYOffset, typeSize.x, typeSize.y);
            Widgets.DrawBoxSolid(typeRect, type.valueColor);
            Widgets.Label(typeLabelRect, label);
            height += 10 + 2;
        }
        Text.Font = default;
        Text.Anchor = default;
        Widgets.EndGroup();

        return height;
    }
    */

    public static void DrawValueContainerReadout<TValue>(Rect rect, ValueContainerBase<TValue> container)
        where TValue : FlowValueDef
    {
        float height = 5;
        Widgets.DrawMenuSection(rect);
        Widgets.BeginGroup(rect);
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperLeft;
        foreach (var type in container.StoredDefs)
        {
            string label =
                $"{type.labelShort}: {container.StoredValueOf(type)} ({container.StoredPercentOf(type).ToStringPercent()})";
            Rect typeRect = new Rect(5, height, 10, 10);
            Vector2 typeSize = Text.CalcSize(label);
            Rect typeLabelRect = new Rect(20, height - 2, typeSize.x, typeSize.y);
            Widgets.DrawBoxSolid(typeRect, type.valueColor);
            Widgets.Label(typeLabelRect, label);
            height += 10 + 2;
        }

        Text.Font = default;
        Text.Anchor = default;
        Widgets.EndGroup();

        if (DebugSettings.godMode)
        {
            if (TWidgets.MouseClickIn(rect, 1))
            {
                FloatMenu menu = new FloatMenu(container.DebugFloatMenuOptions, "", true);
                menu.vanishIfMouseDistant = true;
                Find.WindowStack.Add(menu);
            }
        }
    }

    public static void HoverContainerReadout<TValue>(Rect hoverArea, ValueContainerBase<TValue> container)
        where TValue : FlowValueDef
    {
        if (container == null) return;

        //Draw Hovered Readout
        if (container.FillState != ContainerFillState.Empty && Mouse.IsOver(hoverArea))
        {
            var mousePos = Event.current.mousePosition;
            var containerReadoutSize = GetValueContainerReadoutSize(container);
            Rect rectAtMouse = new Rect(mousePos.x, mousePos.y - containerReadoutSize.y, containerReadoutSize.x,
                containerReadoutSize.y);
            DrawValueContainerReadout(rectAtMouse, container);
        }
    }


    public static void HoverFlowBoxReadout(Rect hoverArea, Flow.FlowBox flowBox)
    {
        if (flowBox == null) return;
        
        if (flowBox.FillState != ContainerFillState.Empty && Mouse.IsOver(hoverArea))
        {
            var mousePos = Event.current.mousePosition;
            var containerReadoutSize = GetFlowBoxReadoutSize(flowBox);
            Rect rectAtMouse = new Rect(mousePos.x, mousePos.y - containerReadoutSize.y, containerReadoutSize.x,
                containerReadoutSize.y);
            DrawFlowBoxReadout(rectAtMouse, flowBox);
        }
    }
}