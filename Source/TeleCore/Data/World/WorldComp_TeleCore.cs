using RimWorld.Planet;
using Verse;

namespace TeleCore;

public class WorldComp_TeleCore : WorldComponent
{
    //Discovery
    internal DiscoveryTable _discoveries;
    internal UISettingsTable _uiSettingsTable;

    public WorldComp_TeleCore(World world) : base(world)
    {
        GenerateInfos();
        StaticData.Notify_NewTeleWorldComp(this);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        StaticData.ExposeStaticData();
        Scribe_Deep.Look(ref _discoveries, "DiscoveryTable");
        Scribe_Deep.Look(ref _uiSettingsTable, "UISettings");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            GenerateInfos();
        }
    }


    private void GenerateInfos()
    {
        _discoveries ??= new DiscoveryTable();
        _uiSettingsTable ??= new UISettingsTable();
    }
}

