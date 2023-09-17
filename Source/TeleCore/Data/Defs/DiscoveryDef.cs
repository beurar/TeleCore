using Verse;

namespace TeleCore;

public class DiscoveryDef : Def
{
    //public WikiEntryDef wikiEntry;

    public void Discover()
    {
        TFind.Discoveries.Discover(this);
    }
}