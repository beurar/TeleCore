using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TeleCore.Static;
using Verse;

namespace TeleCore;

public class DiscoveryTable : IExposable
{
    private Dictionary<DiscoveryDef, bool> discoveries = new Dictionary<DiscoveryDef, bool>();
    private Dictionary<BuildableDef, bool> discoveredMenuOptions = new Dictionary<BuildableDef, bool>();

    //TODO: Research For ALL!
    //public Dictionary<TResearchDef, bool> DiscoveredResearch = new Dictionary<TResearchDef, bool>();

    public bool this[DiscoveryDef discovery] => IsDiscovered(discovery);
    public bool this[IDiscoverable discovery] => this[discovery.DiscoveryDef];
    
    public Dictionary<DiscoveryDef, bool> Discoveries => discoveries;
    public Dictionary<BuildableDef, bool> DiscoveredMenuOptions => discoveredMenuOptions;
    
    public void ExposeData()
    {
        Scribe_Collections.Look(ref discoveries, "discoveredDict");
        Scribe_Collections.Look(ref discoveredMenuOptions, "menuDiscovered");
    }
    
    //
    public bool MenuOptionHasBeenSeen(BuildableDef def)
    {
        return DiscoveredMenuOptions.TryGetValue(def, out bool value) && value;
    }

    public void DiscoverInMenu(BuildableDef def)
    {
        if (MenuOptionHasBeenSeen(def)) return;
        DiscoveredMenuOptions.Add(def, true);
    }
    
    //Parent Discovery
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDiscovered(DiscoveryDef discovery)
    {
        return Discoveries.TryGetValue(discovery, out bool value) && value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDiscovered(IDiscoverable discoverable)
    {
        return IsDiscovered(discoverable.DiscoveryDef);
    }

    public void Discover(DiscoveryDef discovery)
    {
        if (IsDiscovered(discovery)) return;
        Discoveries.Add(discovery, true);
        Find.LetterStack.ReceiveLetter(TranslationUtil.Discovery.DiscoveryNew, TranslationUtil.Discovery.DiscoveryDesc(discovery.description), TeleDefOf.DiscoveryLetter);
    }
    
    // //Research Discovery
    // public bool ResearchHasBeenSeen(TResearchDef research)
    // {
    //     return DiscoveredResearch.TryGetValue(research, out bool value) && value;
    // }
    //
    // public void DiscoverResearch(TResearchDef research)
    // {
    //     if (ResearchHasBeenSeen(research)) return;
    //     DiscoveredResearch.Add(research, true);
    // }
}