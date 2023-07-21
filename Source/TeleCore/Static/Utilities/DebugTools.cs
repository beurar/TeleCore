using RimWorld;
using TeleCore.Network.Flow;
using TeleCore.Network.Graph;
using UnityEngine;
using Verse;

namespace TeleCore.Static.Utilities;

[StaticConstructorOnStartup]
internal static class DebugTools
{
    private static readonly Material FilledMat =
        SolidColorMaterials.NewSolidColorMaterial(Color.green, ShaderDatabase.MetaOverlay);

    private static readonly Material UnFilledMat =
        SolidColorMaterials.NewSolidColorMaterial(TColor.LightBlack, ShaderDatabase.MetaOverlay);

    #region Graph

    internal static void Debug_DrawGraphOnUI(NetGraph graph)
    {
        var size = Find.CameraDriver.CellSizePixels / 4;

        foreach (var netEdge in graph.EdgeLookUp)
        {
            var subParts = netEdge.Key;
            var thingA = subParts.Item1.Value.Thing;
            var thingB = subParts.Item2.Value.Thing;

            //TWidgets.DrawHalfArrow(ScreenPositionOf(thingA.TrueCenter()), ScreenPositionOf(thingB.TrueCenter()), Color.red, 8);

            //TODO: edge access only works for one version (node1, node2) - breaks two-way
            //TODO: some edges probably get setup broken (because only one edge is set)
            if (netEdge.Value.IsValid)
            {
                TWidgets.DrawHalfArrow(netEdge.Value.From.Parent.Thing.TrueCenter().ToScreenPos(),
                    netEdge.Value.To.Parent.Thing.TrueCenter().ToScreenPos(), Color.red, size);
                if (netEdge.Value.BiDirectional)
                    TWidgets.DrawHalfArrow(netEdge.Value.To.Parent.Thing.TrueCenter().ToScreenPos(),
                        netEdge.Value.From.Parent.Thing.TrueCenter().ToScreenPos(), Color.blue, size);
            }

            TWidgets.DrawBoxOnThing(thingA);
            TWidgets.DrawBoxOnThing(thingB);
        }
    }

    internal static void Debug_DrawPressure(NetworkSystem networkSys)
    {
        foreach (var (part, fb) in networkSys.Relations)
        {
            var r = default(GenDraw.FillableBarRequest);
            r.center = part.Parent.Thing.Position.ToVector3() + new Vector3(0.25f, 0, 0.75f);
            r.size = new Vector2(1.5f, 0.5f);
            r.fillPercent = (float) fb.FillPercent;
            r.filledMat = FilledMat;
            r.unfilledMat = UnFilledMat;
            r.margin = 0f;
            r.rotation = Rot4.East;
            GenDraw.DrawFillableBar(r);
        }
    }

    internal static void Debug_DrawOverlays(NetGraph graph)
    {
        foreach (var node in graph.Nodes)
        {
            var pos = node.Value.Thing.DrawPos;
            GenMapUI.DrawText(new Vector2(pos.x, pos.z), $"[{node.Value.Thing}]", Color.green);
        }
    }

    #endregion
}