using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TeleCore;

public class CompProperties_FX : CompProperties
{
    public List<FXEffecterData> effectLayers = new();

    //Layers
    public List<FXLayerData> fxLayers = new();
    public IntRange tickOffset = new(0, 333);

    public CompProperties_FX()
    {
        compClass = typeof(CompFX);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef def)
    {
        if (def.drawerType == DrawerType.MapMeshOnly && Enumerable.Any(fxLayers, o => o.fxMode != FXMode.Static))
            yield return $"{def} has dynamic overlays but is MapMeshOnly";
    }
}