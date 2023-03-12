using System.Collections.Generic;
using Verse;

namespace TeleCore.RWExtended;

public class TeleBuilding : FXBuilding, IDiscoverable
{
    public override string Label => Discovered ? DiscoveredLabel : UnknownLabel;
    public override string DescriptionFlavor => Discovered ? DiscoveredDescription : UnknownDescription;

    public TeleDefExtension Extension { get; private set; }
    public DiscoveryProperties Discovery => Extension?.discovery!;

    public DiscoveryDef DiscoveryDef => Discovery.discoveryDef;
    public string DiscoveredLabel => base.Label;
    public string UnknownLabel => Discovery.UnknownLabelCap;
    public string DiscoveredDescription => def.description;
    public string UnknownDescription => Discovery.unknownDescription;
    public string DescriptionExtra => Discovery.extraDescription;

    public bool IsDiscoverable => Discovery != null;
    public bool Discovered => !IsDiscoverable || TFind.Discoveries[this];

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        Extension = def.TeleExtension();
    }
    
    public override string GetInspectString()
    {
        string str = (IsDiscoverable && !Discovered) ? "TELE.Discovery.NotDiscovered".Translate().ToString() + "\n" : "";
        str += base.GetInspectString();
        return str;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos())
        {
            yield return g;
        }

        if (!DebugSettings.godMode) yield break;
        if (IsDiscoverable && !Discovered)
        {
            yield return new Command_Action()
            {
                defaultLabel = "Discover",
                action = delegate { DiscoveryDef.Discover(); }
            };
        }
    }

}